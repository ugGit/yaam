using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yaam.Infrastructure.Persistence;
using Yaam.UseCases.Common.Email;

namespace Yaam.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
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
