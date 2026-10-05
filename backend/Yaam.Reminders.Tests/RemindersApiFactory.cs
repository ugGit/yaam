using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Notifications;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Tests;

public class RemindersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public CapturingEmailSender EmailSender { get; } = new();

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = TestTokenHelper.GenerateToken(TestUserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task SeedSnapshotAsync(ApplicationSnapshot snapshot)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        db.ApplicationSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
    }

    public static ApplicationSnapshot NewSnapshot(string companyName) => new()
    {
        ApplicationId = Guid.NewGuid(),
        Version = 1,
        CompanyName = companyName,
        Role = "Developer",
        DateApplied = new DateOnly(2026, 9, 1),
        ContactName = "Grace",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"),
                optional: true);
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices((context, services) =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RemindersDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<RemindersDbContext>(options =>
                options.UseNpgsql(
                    context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Test connection string 'DefaultConnection' not found in appsettings.Test.json.")));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);

            var notificationJob = services.Single(d => d.ImplementationType == typeof(ReminderNotificationService));
            services.Remove(notificationJob);

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TestTokenHelper.PublicKey,
                    ValidateIssuer = true,
                    ValidIssuer = TestTokenHelper.Issuer,
                    ValidateAudience = true,
                    ValidAudience = TestTokenHelper.Audience,
                    ValidateLifetime = true,
                };
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}

public record SentEmail(string To, string Subject, string Body);

public class CapturingEmailSender : IEmailSender
{
    private readonly List<SentEmail> _sent = [];

    public IReadOnlyList<SentEmail> Sent
    {
        get { lock (_sent) return _sent.ToList(); }
    }

    public void Clear()
    {
        lock (_sent) _sent.Clear();
    }

    public Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken)
    {
        lock (_sent) _sent.Add(new SentEmail(to, subject, textBody));
        return Task.CompletedTask;
    }
}

public static class TestTokenHelper
{
    internal const string Issuer = "https://test.supabase.co/auth/v1";
    internal const string Audience = "authenticated";

    private static readonly RSA Rsa = RSA.Create(2048);
    private static readonly RsaSecurityKey SigningKey = new(Rsa);
    internal static readonly RsaSecurityKey PublicKey = new(Rsa.ExportParameters(includePrivateParameters: false));

    public static string GenerateToken(Guid userId)
    {
        var credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
