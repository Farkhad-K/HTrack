using HTrack.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HTrack.Api.Data;

public interface IHTrackDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Company> Companies { get; set; }
    DbSet<Employee> Employees { get; set; }
    DbSet<Attendance> Attendances { get; set; }
    ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}