using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Yaam.Contracts;
using Yaam.ServiceDefaults.Messaging;

namespace Yaam.Reminders.ApplicationEvents;

public sealed class ApplicationEventsConsumer(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connectionProvider,
    ILogger<ApplicationEventsConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilChannelClosesAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Application event consumer disconnected; reconnecting in {Delay}", ReconnectDelay);
            }

            await Task.Delay(ReconnectDelay, stoppingToken);
        }
    }

    private async Task ConsumeUntilChannelClosesAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await ApplicationEventsTopology.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var closed = new TaskCompletionSource();
        channel.ChannelShutdownAsync += (_, _) =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(ApplicationEventsTopology.Queue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming {Queue}", ApplicationEventsTopology.Queue);

        await closed.Task.WaitAsync(stoppingToken);
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<MessageEnvelope>(delivery.Body.Span, ContractSerializer.Options)
                ?? throw new JsonException("Empty message body.");

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ApplicationEventHandler>();
            var outcome = await handler.HandleAsync(envelope, cancellationToken);

            logger.LogInformation("Message {MessageId} ({Type}) → {Outcome}", envelope.MessageId, envelope.Type, outcome);
            // Ack only after the DB commit: a crash before this line means redelivery, which the inbox absorbs.
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to process message {MessageId}", delivery.BasicProperties.MessageId);
            // requeue: the quorum queue counts deliveries and dead-letters after x-delivery-limit.
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }
}
