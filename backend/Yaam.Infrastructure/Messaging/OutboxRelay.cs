using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using RabbitMQ.Client;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Persistence.Outbox;
using Yaam.ServiceDefaults.Messaging;
using Yaam.ServiceDefaults.Telemetry;

namespace Yaam.Infrastructure.Messaging;

public sealed class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connectionProvider,
    IOptions<OutboxSettings> options,
    OutboxMetrics metrics,
    ILogger<OutboxRelay> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(options.Value.PollingIntervalMilliseconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Outbox relay iteration failed; retrying in {Interval}", interval);
            }

            // Measured independently of publishing: the backlog matters most exactly when publishing fails.
            try
            {
                await RecordBacklogAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not measure outbox backlog");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var batchSize = options.Value.BatchSize;
        List<OutboxMessage> pending;
        // Polling every second would otherwise create a root trace per poll and bury real traces.
        using (SuppressInstrumentationScope.Begin())
        {
            // SKIP LOCKED lets several relay instances share the table without publishing the same row concurrently.
            pending = await db.OutboxMessages
                .FromSql($"""SELECT * FROM "OutboxMessages" WHERE "ProcessedAt" IS NULL ORDER BY "Id" LIMIT {batchSize} FOR UPDATE SKIP LOCKED""")
                .ToListAsync(cancellationToken);
        }
        if (pending.Count == 0)
            return;

        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await channel.ExchangeDeclareAsync(
            ApplicationTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await PublishAsync(channel, message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Attempts++;
                logger.LogWarning(exception,
                    "Publishing outbox message {MessageId} failed (attempt {Attempts})", message.MessageId, message.Attempts);
                // Stop the batch: publishing a later message before an earlier one would break per-aggregate ordering.
                break;
            }

            if (options.Value.SimulateCrashAfterPublish)
                throw new InvalidOperationException("Simulated crash after publish, before marking processed.");

            message.ProcessedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RecordBacklogAsync(CancellationToken cancellationToken)
    {
        using var suppressTracing = SuppressInstrumentationScope.Begin();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = db.OutboxMessages.Where(m => m.ProcessedAt == null);
        var count = await pending.LongCountAsync(cancellationToken);
        var oldest = await pending.MinAsync(m => (DateTime?)m.OccurredAt, cancellationToken);
        metrics.Update(count, oldest is null ? 0 : (DateTime.UtcNow - oldest.Value).TotalSeconds);
    }

    private static async Task PublishAsync(IChannel channel, OutboxMessage message, CancellationToken cancellationToken)
    {
        using var payload = JsonDocument.Parse(message.Payload);
        var envelope = new MessageEnvelope(
            message.MessageId, message.Type, new DateTimeOffset(message.OccurredAt, TimeSpan.Zero), payload.RootElement);
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, ContractSerializer.Options);
        var properties = new BasicProperties
        {
            MessageId = message.MessageId.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>(),
        };

        using var activity = MessagingTelemetry.StartPublishActivity(message.TraceParent, message.Type);
        if (activity is not null)
            properties.Headers[MessagingTelemetry.TraceParentHeader] = activity.Id;

        // mandatory: an unroutable message (no queue bound yet) fails the publish instead of vanishing.
        await channel.BasicPublishAsync(
            ApplicationTopology.Exchange,
            ApplicationTopology.RoutingKeyFor(message.Type),
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
