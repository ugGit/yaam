using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Yaam.Infrastructure.Persistence;
using Yaam.UseCases.Common.Cv;
using Yaam.UseCases.Common.Email;

namespace Yaam.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = TestTokenHelper.GenerateToken(TestUserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

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
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Test connection string 'DefaultConnection' not found in appsettings.Test.json.")));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();

            services.RemoveAll<ICvParser>();
            services.AddSingleton<ICvParser, NoOpCvParser>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(TestTokenHelper.Secret));
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}

public class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class NoOpCvParser : ICvParser
{
    public Task<ParsedCvDto> ParseAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(new ParsedCvDto(
            new ParsedInfoDto("Ada", "Lovelace", "ada@example.com", "+41 79 000 00 00", null, null),
            [new ParsedWorkExperienceDto("Acme", "Engineer", "2020-01", null, "Built things.")],
            [],
            ["C#", "Angular"],
            [new ParsedLanguageDto("English", "Native")],
            [],
            []));
}

public static class TestTokenHelper
{
    // PostConfigure<JwtBearerOptions> in ApiFactory uses these constants as the source of truth; appsettings.Test.json Supabase section is kept for documentation only.
    internal const string Secret = "test-jwt-secret-minimum-32-chars-long!!";
    internal const string Issuer = "https://test.supabase.co/auth/v1";
    internal const string Audience = "authenticated";

    public static string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
