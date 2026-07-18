namespace Yaam.UseCases.Common.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "YAAM";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string NotificationEmail { get; set; } = string.Empty;
}
