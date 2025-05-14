using Bogus;
using HTrack.Api.Data;
using HTrack.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HTrack.Api.Data;

public class MigrationsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<MigrationsHostedService> logger,
    IConfiguration configuration) : IHostedService
{
    private IHTrackDbContext context = default!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        context = scope.ServiceProvider.GetRequiredService<IHTrackDbContext>();
        if (configuration.GetValue<bool>("MigrateDatabase"))
        {
            logger.LogInformation("Migrating database.");
            await context.Database.MigrateAsync(cancellationToken);
        }
        if (configuration.GetValue<bool>("SeedData") && !await context.Companies.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seeding test data.");
            await SeedData(cancellationToken);
        }
    }

    private async Task SeedData(CancellationToken cancellationToken)
    {
        // Create Companies
        var company1 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Ilmhub Namangan",
            TgChatID = -4620866435,
            ManagerTgUserIDs = [981852210,6713958985]
        };

        var company2 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Ilmhub Uychi",
            TgChatID = -4645032412,
            ManagerTgUserIDs = [5774402094]
        };

        var employeeFaker = new Faker<Employee>()
            .RuleFor(e => e.Id, f => Guid.NewGuid())
            .RuleFor(e => e.Name, f => f.Name.FullName())
            .RuleFor(e => e.RFIDCardUID, f => f.Random.Hexadecimal(8).Replace("0x", "").ToUpper())
            .RuleFor(e => e.Company, f => f.PickRandom(company1, company2));

        var employees = employeeFaker.Generate(10);

        // Attendance Faker with check-in across the last 6 months and working hours
        var attendanceFaker = new Faker<Attendance>()
            .RuleFor(a => a.Id, f => Guid.NewGuid())
            .RuleFor(a => a.CheckIn, f =>
            {
                var date = f.Date.Between(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow);
                var hour = f.Random.Int(8, 12); // Morning shift
                var minute = f.Random.Int(0, 59);
                var second = f.Random.Int(0, 59);
                return new DateTime(date.Year, date.Month, date.Day, hour, minute, second, DateTimeKind.Utc);
            })
            .RuleFor(a => a.Duration, f => TimeSpan.FromHours(f.Random.Double(4, 9)))
            .FinishWith((f, a) =>
            {
                a.CheckOut = a.CheckIn + a.Duration;
            });

        var attendances = new List<Attendance>();
        var random = new Random();

        foreach (var emp in employees)
        {
            var count = random.Next(5, 15); // More entries per employee
            for (int i = 0; i < count; i++)
            {
                var att = attendanceFaker.Generate();
                att.EmployeeId = emp.Id;
                attendances.Add(att);
            }
        }

        context.Companies.AddRange(company1, company2);
        context.Employees.AddRange(employees);
        context.Attendances.AddRange(attendances);

        await context.SaveChangesAsync(cancellationToken);
    }


    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
