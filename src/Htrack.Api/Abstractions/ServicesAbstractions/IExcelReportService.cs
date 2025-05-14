using Microsoft.AspNetCore.Mvc;

public interface IExcelReportService
{
    Task GenerateMonthlyAttendanceReportsAsync(CancellationToken cancellationToken = default);
    Task<FileStreamResult?> GetLastMonthReportAsync(Guid companyId);

    Task Generate15DayAttendanceReportsAsync(CancellationToken cancellationToken = default);
    Task<FileStreamResult?> Get15DayReportAsync(Guid companyId);
}
