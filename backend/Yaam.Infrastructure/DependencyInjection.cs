using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Repositories;

namespace Yaam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();

        return services;
    }
}
