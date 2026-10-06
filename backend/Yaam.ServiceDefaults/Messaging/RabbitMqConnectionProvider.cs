using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Yaam.ServiceDefaults.Messaging;

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqSettings> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                ClientProvidedName = settings.ClientName,
                // The relay and consumer loops own reconnection; one recovery mechanism is easier to reason about.
                AutomaticRecoveryEnabled = false,
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
