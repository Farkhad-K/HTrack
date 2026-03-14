public interface IExcelReportService
{
    Task<(MemoryStream Stream, string FileName)> GetLastMonthReportAsync(Guid companyId, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> Get15DayReportAsync(Guid companyId, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetFromStartToTodayAsync(Guid companyId, CancellationToken ct = default);
    Task<(MemoryStream Stream, string FileName)> GetCustomRangeReportAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
