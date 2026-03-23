using HTrack.Api.Entities;
using HTrack.Api.Utilities;

namespace HTrack.Api.Services;

public partial class ExcelReportService
{
    private static readonly TimeSpan LongShiftThreshold = TimeSpan.FromHours(16);
    private static readonly TimeSpan DurationMismatchTolerance = TimeSpan.FromMinutes(1);

    private static string? GetAnomalyLabel(AttendanceRow row)
    {
        List<string> flags = [];

        if (row.Duration < TimeSpan.Zero)
            flags.Add("Manfiy davomiylik");

        if (row.CheckOut.HasValue)
        {
            if (row.CheckOut.Value < row.CheckIn)
                flags.Add("Ketish vaqti noto'g'ri");

            var computedDuration = row.CheckOut.Value - row.CheckIn;
            if (computedDuration >= TimeSpan.Zero &&
                Math.Abs((computedDuration - row.Duration).TotalMinutes) >= DurationMismatchTolerance.TotalMinutes)
            {
                flags.Add("Davomiylik mos emas");
            }

            if (row.Duration > LongShiftThreshold)
                flags.Add($"Uzoq smena ({FormatDuration(row.Duration)})");
        }

        return flags.Count == 0 ? null : string.Join("; ", flags);
    }

    private static bool HasAnomaly(AttendanceRow row)
        => GetAnomalyLabel(row) is not null;

    private static bool HasManualEntry(AttendanceRow row)
        => row.CheckInSource == AttendanceEntrySource.Manual || row.CheckOutSource == AttendanceEntrySource.Manual;

    private static string? GetSourceLabel(AttendanceRow row)
    {
        var checkInManual = row.CheckInSource == AttendanceEntrySource.Manual;
        var checkOutManual = row.CheckOutSource == AttendanceEntrySource.Manual;

        return (checkInManual, checkOutManual) switch
        {
            (true, true) => "Qo'lda kelish/ketish",
            (true, false) => "Qo'lda kelish",
            (false, true) => "Qo'lda ketish",
            _ => null
        };
    }

    private int AppendManualSummarySection(ClosedXML.Excel.IXLWorksheet ws, List<AttendanceRow> rows, int startRow)
    {
        var manualRows = rows
            .Where(HasManualEntry)
            .OrderBy(r => TimeHelper.ToUzbekistanTime(r.CheckIn))
            .ToList();

        if (manualRows.Count == 0)
            return startRow;

        var row = startRow + 1;
        ws.Cell(row, 1).Value = "Qo'lda yozuvlar";
        ws.Range(row, 1, row, 6).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#D9EAF7");
        row++;

        ws.Cell(row, 1).Value = "Xodim";
        ws.Cell(row, 2).Value = "RFID";
        ws.Cell(row, 3).Value = "Kelish";
        ws.Cell(row, 4).Value = "Ketish";
        ws.Cell(row, 5).Value = "Manba";
        ws.Cell(row, 6).Value = "Soat";
        ws.Range(row, 1, row, 6).Style.Font.Bold = true;
        ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
        row++;

        foreach (var manualRow in manualRows)
        {
            var checkIn = TimeHelper.ToUzbekistanTime(manualRow.CheckIn);
            var checkOut = manualRow.CheckOut.HasValue ? TimeHelper.ToUzbekistanTime(manualRow.CheckOut.Value) : (DateTime?)null;

            ws.Cell(row, 1).Value = manualRow.EmployeeName;
            ws.Cell(row, 2).Value = manualRow.RFID;
            ws.Cell(row, 3).Value = checkIn.ToString("dd.MM.yyyy HH:mm");
            ws.Cell(row, 4).Value = checkOut?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            ws.Cell(row, 5).Value = GetSourceLabel(manualRow) ?? "";
            ws.Cell(row, 6).Value = FormatDuration(manualRow.Duration);
            row++;
        }

        return row;
    }
}
