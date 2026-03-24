using HTrack.Api.Data;
using HTrack.Api.Entities;
using HTrack.Api.Exceptions;
using HTrack.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Htrack.Api.Tests;

public class EmployeesRepositoryTests
{
    [Fact]
    public async Task UpdateRfidAsync_NormalizesUidBeforeSaving()
    {
        var companyId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Co",
            TgChatID = 1
        });
        db.Employees.Add(new Employee
        {
            Id = employeeId,
            CompanyId = companyId,
            Name = "Ali Valiyev",
            RFIDCardUID = "ABC123"
        });
        await ((IHTrackDbContext)db).SaveChangesAsync();

        var repository = new EmployeesRepository(db, NullLogger<CompaniesRepository>.Instance);

        var updated = await repository.UpdateRfidAsync(companyId, "ABC123", " aa bb cc dd ");

        Assert.Equal("AABBCCDD", updated.RFIDCardUID);
        Assert.Equal("AABBCCDD", await db.Employees.Where(e => e.Id == employeeId).Select(e => e.RFIDCardUID).SingleAsync());
    }

    [Fact]
    public async Task UpdateRfidAsync_ThrowsWhenUidAlreadyBelongsToAnotherEmployee()
    {
        var companyId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Co",
            TgChatID = 1
        });
        db.Employees.AddRange(
            new Employee
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Ali Valiyev",
                RFIDCardUID = "ABC123"
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Vali Aliyev",
                RFIDCardUID = "XYZ789"
            });
        await ((IHTrackDbContext)db).SaveChangesAsync();

        var repository = new EmployeesRepository(db, NullLogger<CompaniesRepository>.Instance);

        await Assert.ThrowsAsync<EmployeeWithUIDAlreadyExistsException>(() =>
            repository.UpdateRfidAsync(companyId, "ABC123", " xyz789 ").AsTask());
    }

    private static HTrackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HTrackDbContext(options);
    }
}
