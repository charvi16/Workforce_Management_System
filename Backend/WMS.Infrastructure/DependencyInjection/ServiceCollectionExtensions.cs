using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WmsDatabase");

        services.AddDbContext<WmsDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
