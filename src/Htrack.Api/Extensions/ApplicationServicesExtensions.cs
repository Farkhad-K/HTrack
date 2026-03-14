using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Repositories;
using HTrack.Api.Services;

namespace HTrack.Api.Extensions;

internal static class ApplicationServicesExtensions
{
    internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICompaniesRepository, CompaniesRepository>();
        services.AddScoped<IEmployeesRepository, EmployeesRepository>();
        services.AddScoped<IAttendancesRepository, AttendancesRepository>();

        services.AddScoped<ICompaniesService, CompaniesService>();
        services.AddScoped<IEmployeesService, EmployeesService>();
        services.AddScoped<IAttendancesService, AttendancesService>();
        services.AddScoped<IExcelReportService, ExcelReportService>();

        services.AddHostedService<AttendanceCleanupService>();

        return services;
    }
}
