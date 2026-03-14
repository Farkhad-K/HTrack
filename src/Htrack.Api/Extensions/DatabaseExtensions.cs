using HTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Extensions;

internal static class DatabaseExtensions
{
    internal static IServiceCollection AddDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IHTrackDbContext, HTrackDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("HTrack")));

        services.AddHostedService<MigrationsHostedService>();

        return services;
    }
}
