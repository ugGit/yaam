using RabbitMQ.Client;
using Yaam.Contracts.Applications;

namespace Yaam.Reminders.ApplicationEvents;

// The consumer owns its queue; the producer only knows the exchange.
internal static class ApplicationEventsTopology
{
    internal const string Queue = "reminders.application-events";
    internal const string DeadLetterExchange = "reminders.dead-letter";
    internal const string DeadLetterQueue = "reminders.application-events.dlq";

    internal static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            ApplicationTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: string.Empty,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = 5,
                ["x-dead-letter-exchange"] = DeadLetterExchange,
            },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(Queue, ApplicationTopology.Exchange, routingKey: "application.*",
            cancellationToken: cancellationToken);
    }
}
