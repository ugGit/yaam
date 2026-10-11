namespace Yaam.ServiceDefaults.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
}
