using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;

namespace WMS.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WmsDatabase");

        services.AddDbContext<WmsDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
