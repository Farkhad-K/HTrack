using Telegram.Bot;
using Telegram.Bot.Types;
using HTrack.Api.Entities;
using Telegram.Bot.Types.Enums;
using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Utilities;

namespace HTrack.Api.TelegramBotServices;

public partial class BotUpdateHandler
{
    private static async Task HandleStartCommand(ITelegramBotClient botClient, Message message,
        Company? userCompany, CancellationToken ct)
    {
        var from = message.From!;
        var greeting_HelpMsg =
            "✨ HTrack Botiga xush kelibsiz! ✨\n\n" +
            "Quyidagi buyruqlardan foydalanishingiz mumkin:\n\n" +
            "🔹 */start* - Xush kelibsiz xabari va kompaniya ruxsati\n" +
            "🔹 */employees* - Barcha xodimlar va ularning RFID kodlari ro‘yxati\n" +
            "🔹 */excel_report* - O‘tgan oy uchun Excel hisobotini yuklab olish\n" +
            "🔹 */15daysreport* - So‘nggi 15 kunlik tashriflar hisobotini yuklab olish\n" +
            "🔹 */new_attendance* - RFID orqali xodimni qo‘lda ro‘yxatdan o‘tkazish\n" +
            "🔹 */update_employee* - Xodim ismini RFID orqali yangilash\n" +
            "🔹 */checked_in* - Hozir ishda bo‘lgan xodimlar ro‘yxati\n" +
            "🔹 */checked_out* - Bugun ishni tugatgan xodimlar ro‘yxati\n\n" +
            "🔹 */report_till_today* - Ushbu buyruq orqali oyning 1-chisidan bugungacha bo'lgan ishchilarni ish vaqti yozilgan excel hisobotini olish mumkin\n\n" +
            "ℹ️ Yuqoridagi buyruqlar yordamida kompaniyangizning tashrif tizimi bilan samarali ishlang.";

        var welcomeText = userCompany is not null
            ? $"👋 Assalomu alaykum, {from.FirstName}! Siz *{userCompany.Name}* kompaniyasiga ruxsatga egasiz.\n\n{greeting_HelpMsg}"
            : $"👋 Assalomu alaykum, {from.FirstName}! Siz hech qanday kompaniyaga ruxsatga ega emassiz.";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: welcomeText,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private static async Task HandleEmployeesCommand(
        ITelegramBotClient botClient, Message message, Company? userCompany,
        IEmployeesRepository employeesRepository, CancellationToken ct)
    {
        var employees = await employeesRepository.GetAllAsync(userCompany!.Id, ct);
        var lines = employees.Select(e => $"• {e.Name} (RFID: `{e.RFIDCardUID}`)");
        var messageText = "👥 Xodimlar ro‘yxati:\n" + string.Join("\n", lines);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: messageText,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private static async Task HandleExcelReportCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var reportResult = await reportService.GetLastMonthReportAsync(userCompany!.Id);

        if (reportResult is null)
        {
            await reportService.GenerateMonthlyAttendanceReportsAsync(ct);
            reportResult = await reportService.GetLastMonthReportAsync(userCompany.Id);

            if (reportResult is null)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ O‘tgan oy uchun hisobot mavjud emas.",
                    cancellationToken: ct);
                return;
            }
        }

        var fileStream = reportResult.FileStream;
        var fileName = reportResult.FileDownloadName;

        await botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(fileStream, fileName),
            caption: $"📊 {userCompany.Name} kompaniyasining o'tgan oy uchun, tashrif hisobot fayli",
            cancellationToken: ct);
    }


    private static async Task Handle15DaysReportCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var report15 = await reportService.Get15DayReportAsync(userCompany!.Id);

        if (report15 is null)
        {
            await reportService.Generate15DayAttendanceReportsAsync(ct);
            report15 = await reportService.Get15DayReportAsync(userCompany.Id);

            if (report15 is null)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ So‘nggi 15 kunlik hisobotni yaratish uchun ma’lumot topilmadi.",
                    cancellationToken: ct);
                return;
            }
        }

        await botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(report15.FileStream, report15.FileDownloadName),
            caption: $"📆 {userCompany.Name} kompaniyasi uchun 15 kunlik tashrif hisobot fayli",
            cancellationToken: ct);
    }

    private static async Task HandleReportTillTodayCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var report = await reportService.GetFromStartToTodayAsync(userCompany!.Id);

        if (report is null)
        {
            await reportService.GenerateReportFromStartToTodayAsync(ct);
            report = await reportService.GetFromStartToTodayAsync(userCompany.Id);

            if (report is null)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ Bugungu kungacha hisobotni yaratish uchun ma’lumot topilmadi.",
                    cancellationToken: ct);
                return;
            }
        }

        await botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(report.FileStream, report.FileDownloadName),
            caption: $"📆 {userCompany.Name} kompaniyasi uchun 1-dan bugungacha tashrif hisobot fayli",
            cancellationToken: ct);
    }

    private static async Task HandleCheckedInCommand(
    ITelegramBotClient botClient,
    Message message,
    Company? userCompany,
    IAttendancesRepository attendancesRepository,
    CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var checkedInEmployees = await attendancesRepository.GetAllCheckInAsync(userCompany!.Id, ct);
        if (!checkedInEmployees.Any())
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "ℹ️ Hozirda hech bir xodim ishda emas.",
                cancellationToken: ct);
        }
        else
        {
            var inLines = checkedInEmployees.Select(a =>
            {
                var uzTime = TimeHelper.ToUzbekistanTime(a!.CheckIn);
                return $"• {a!.Employee!.Name} (RFID: `{a.Employee.RFIDCardUID}`) at {uzTime:HH:mm:ss}";
            });

            var checkedInText = "✅ *Hozirda ishda bo‘lgan xodimlar:*\n" + string.Join("\n", inLines);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: checkedInText,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
    }

    private static async Task HandleCheckedOutCommand(
    ITelegramBotClient botClient,
    Message message,
    Company? userCompany,
    IAttendancesRepository attendancesRepository,
    CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var checkedOutEmployees = await attendancesRepository.GetAllCheckOutAsync(userCompany!.Id, ct);
        if (!checkedOutEmployees.Any())
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "ℹ️ Hozircha hech bir xodim ishni yakunlamagan.",
                cancellationToken: ct);
        }
        else
        {
            var outLines = checkedOutEmployees
                .Select(a =>
                {
                    var uzTime = TimeHelper.ToUzbekistanTime(a!.CheckOut!.Value);
                    return $"• {a!.Employee!.Name} (RFID: `{a.Employee.RFIDCardUID}`) at {uzTime:HH:mm:ss}";
                });
            var checkedOutText = "🏁 *Bugun ishni tugatgan xodimlar:*\n" + string.Join("\n", outLines);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: checkedOutText,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
    }

}