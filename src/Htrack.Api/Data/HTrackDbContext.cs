using HTrack.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HTrack.Api.Data;

public class HTrackDbContext(
    DbContextOptions<HTrackDbContext> options)
    : DbContext(options), IHTrackDbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Attendance> Attendances { get; set; }

    async ValueTask<int> IHTrackDbContext.SaveChangesAsync(CancellationToken cancellationToken)
        => await base.SaveChangesAsync(cancellationToken);
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HTrackDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
