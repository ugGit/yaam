namespace Yaam.UseCases.Common.Email;

public class EmailSettings
{
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "YAAM";
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string NotificationEmail { get; init; } = string.Empty;
    public bool AllowInsecureConnection { get; init; } = false;
}
