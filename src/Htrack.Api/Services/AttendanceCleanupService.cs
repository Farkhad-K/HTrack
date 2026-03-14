using HTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Services;

public class AttendanceCleanupService(IServiceScopeFactory scopeFactory, ILogger<AttendanceCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IHTrackDbContext>();
            var cutoff = DateTime.UtcNow.AddMonths(-6);
            var deleted = await db.Attendances
                .Where(a => a.CheckIn < cutoff)
                .ExecuteDeleteAsync(stoppingToken);
            logger.LogInformation(
                "Cleanup: deleted {Count} attendance records older than {Cutoff:yyyy-MM-dd}",
                deleted, cutoff);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
