using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using HTrack.Api.Entities;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Utilities;

namespace HTrack.Api.TelegramBotServices;

public partial class BotUpdateHandler
{
    private static readonly ReplyKeyboardMarkup MainKeyboard = new(
    [
        new KeyboardButton[] { "👥 Xodimlar", "✅ Ishda", "🚪 Ishdan chiqdi" },
        new KeyboardButton[] { "📊 O'tgan oy", "📆 Bugunga", "📋 Xodim oylik" },
        new KeyboardButton[] { "📅 15 kunlik", "🗓 Ixtiyoriy sana", "✏️ Davomat", "🔄 Yangilash" }
    ])
    {
        ResizeKeyboard = true,
        IsPersistent = true
    };

    private static async Task HandleStartCommand(ITelegramBotClient botClient, Message message,
        Company? userCompany, CancellationToken ct)
    {
        var from = message.From!;
        var greetingHelpMsg =
            "✨ HTrack Botiga xush kelibsiz! ✨\n\n" +
            "Quyidagi tugmalar yoki buyruqlardan foydalanishingiz mumkin:\n\n" +
            "🔹 */employees* - Barcha xodimlar va ularning RFID kodlari ro'yxati\n" +
            "🔹 */excel_report* - O'tgan oy uchun Excel hisobotini yuklab olish\n" +
            "🔹 */15daysreport* - Joriy 15 kunlik kompaniya hisobotini yuklab olish\n" +
            "🔹 */report_till_today* - Xodimning oy boshidan bugungacha hisobotini olish\n" +
            "🔹 */company_report_till_today* - Kompaniya uchun oy boshidan bugungacha hisobot\n" +
            "🔹 */custom_report* - Ixtiyoriy sana oralig'i hisoboti\n" +
            "🔹 */new_attendance* - RFID orqali xodimni qo'lda ro'yxatdan o'tkazish\n" +
            "🔹 */update_employee* - Xodim ismini RFID orqali yangilash\n" +
            "🔹 */checked_in* - Hozir ishda bo'lgan xodimlar ro'yxati\n" +
            "🔹 */checked_out* - Bugun ishni tugatgan xodimlar ro'yxati\n" +
            "🔹 */cancel* - Joriy amalni bekor qilish";

        var welcomeText = userCompany is not null
            ? $"👋 Assalomu alaykum, {from.FirstName}! Siz *{userCompany.Name}* kompaniyasiga ruxsatga egasiz.\n\n{greetingHelpMsg}"
            : $"👋 Assalomu alaykum, {from.FirstName}! Siz hech qanday kompaniyaga ruxsatga ega emassiz.";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: welcomeText,
            parseMode: ParseMode.Markdown,
            replyMarkup: MainKeyboard,
            cancellationToken: ct);
    }

    private static async Task HandleEmployeesCommand(
        ITelegramBotClient botClient, Message message, Company? userCompany,
        IEmployeesRepository employeesRepository, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var employees = await employeesRepository.GetAllAsync(userCompany!.Id, ct);
        var lines = employees.Select(e => $"• {e.Name} (RFID: `{e.RFIDCardUID}`)");
        var messageText = "👥 Xodimlar ro'yxati:\n" + string.Join("\n", lines);

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

        var (stream, fileName) = await reportService.GetLastMonthReportAsync(userCompany!.Id, ct);

        await botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(stream, fileName),
            caption: $"📊 {userCompany.Name} kompaniyasining o'tgan oy uchun davomat hisoboti",
            cancellationToken: ct);
    }

    private static async Task Handle15DaysReportCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var (stream, fileName) = await reportService.Get15DayReportAsync(userCompany!.Id, ct);

        await botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(stream, fileName),
            caption: $"📅 {userCompany.Name} kompaniyasining joriy 15 kunlik batafsil davomat hisoboti",
            cancellationToken: ct);
    }

    private async Task HandleEmployeeMonthlyCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany, long userId, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        pendingCommands[userId] = "awaitingRfidForMonthToDate";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "📋 Xodimning RFID kodini kiriting:",
            cancellationToken: ct);
    }

    private async Task HandleReportTillTodayCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany,
        long userId, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        pendingCommands[userId] = "awaitingRfidForMonthToDate";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "📆 Xodimning RFID kodini kiriting:",
            cancellationToken: ct);
    }

    private static async Task HandleCompanyReportTillTodayCommand(
        ITelegramBotClient botClient, Message message,
        Company? userCompany,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        var (stream, fileName) = await reportService.GetFromStartToTodayAsync(userCompany!.Id, ct);

        await botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(stream, fileName),
            caption: $"📆 {userCompany.Name} kompaniyasi uchun oy boshidan bugungacha batafsil davomat hisoboti",
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

            var checkedInText = "✅ *Hozirda ishda bo'lgan xodimlar:*\n" + string.Join("\n", inLines);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: checkedInText,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
    }

    private async Task HandleCancelCommand(
        ITelegramBotClient botClient, Message message, long userId, CancellationToken ct)
    {
        ClearUserPendingState(userId);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "✅ Amal bekor qilindi.",
            replyMarkup: MainKeyboard,
            cancellationToken: ct);
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
