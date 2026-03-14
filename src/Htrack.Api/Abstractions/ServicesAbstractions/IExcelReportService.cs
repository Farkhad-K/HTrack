public interface IExcelReportService
{
    Task<(MemoryStream Stream, string FileName)> GetLastMonthReportAsync(Guid companyId, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> Get15DayReportAsync(Guid companyId, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetFromStartToTodayAsync(Guid companyId, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetCustomRangeReportAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetEmployeeMonthToDateReportAsync(Guid companyId, string rfidCardUID, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetEmployee15DayReportAsync(Guid companyId, string rfidCardUID, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetEmployeeCustomRangeReportAsync(Guid companyId, string rfidCardUID, DateOnly from, DateOnly to, CancellationToken ct = default);
}
