using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Yaam.Domain.Repositories;
using Yaam.Domain.Services;
using Yaam.Infrastructure.Auth;
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

        var cvSettings = configuration.GetSection("Cv").Get<CvSettings>() ?? new CvSettings();
        services.AddHttpClient<ICvParser, OllamaCvParser>(client =>
            client.Timeout = TimeSpan.FromSeconds(cvSettings.TimeoutSeconds));
        services.Configure<CvSettings>(configuration.GetSection("Cv"));

        return services;
    }

    public static IServiceCollection AddYaamAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("Supabase").Get<SupabaseSettings>()
            ?? throw new InvalidOperationException("Supabase configuration section is required.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{settings.Url}/auth/v1";
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{settings.Url}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                };
            });

        return services;
    }
}
