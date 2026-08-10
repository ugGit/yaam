using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Cv;
using Yaam.Infrastructure.Email;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Repositories;
using Yaam.UseCases.Common.Cv;
using Yaam.UseCases.Common.Email;

namespace Yaam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();

        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.Configure<CvSettings>(configuration.GetSection("Cv"));
        services.AddHttpClient<ICvParser, OllamaCvParser>();

        return services;
    }
}
