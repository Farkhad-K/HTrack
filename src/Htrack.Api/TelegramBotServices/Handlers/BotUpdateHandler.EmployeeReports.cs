using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HTrack.Api.TelegramBotServices;

public partial class BotUpdateHandler
{
    private async Task HandleAwaitingRfidFor15Day(
        ITelegramBotClient botClient, Message message,
        Company? userCompany, long userId, string text,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
        {
            ClearUserPendingState(userId);
            return;
        }

        var rfidUid = NormaliseRfid(text);
        try
        {
            var (stream, fileName) = await reportService.GetEmployee15DayReportAsync(userCompany!.Id, rfidUid, ct);
            ClearUserPendingState(userId);
            await botClient.SendDocument(
                chatId: message.Chat.Id,
                document: new InputFileStream(stream, fileName),
                caption: "📅 15 kunlik xodim davomat hisoboti",
                cancellationToken: ct);
        }
        catch
        {
            retryCounters.AddOrUpdate(userId, 1, (_, c) => c + 1);
            if (retryCounters.TryGetValue(userId, out var attempts) && attempts >= 3)
            {
                ClearUserPendingState(userId);
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "❌ 3 marta xato kiritdingiz. Buyruqni qaytadan boshlang.", cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: $"❌ Ushbu RFID bo'yicha xodim topilmadi: `{rfidUid}`. Qaytadan kiriting.",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
        }
    }

    private async Task HandleAwaitingRfidForMonthToDate(
        ITelegramBotClient botClient, Message message,
        Company? userCompany, long userId, string text,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
        {
            ClearUserPendingState(userId);
            return;
        }

        var rfidUid = NormaliseRfid(text);
        try
        {
            var (stream, fileName) = await reportService.GetEmployeeMonthToDateReportAsync(userCompany!.Id, rfidUid, ct);
            ClearUserPendingState(userId);
            await botClient.SendDocument(
                chatId: message.Chat.Id,
                document: new InputFileStream(stream, fileName),
                caption: "📋 Xodim oylik davomat hisoboti",
                cancellationToken: ct);
        }
        catch
        {
            retryCounters.AddOrUpdate(userId, 1, (_, c) => c + 1);
            if (retryCounters.TryGetValue(userId, out var attempts) && attempts >= 3)
            {
                ClearUserPendingState(userId);
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "❌ 3 marta xato kiritdingiz. Buyruqni qaytadan boshlang.", cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: $"❌ Ushbu RFID bo'yicha xodim topilmadi: `{rfidUid}`. Qaytadan kiriting.",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
        }
    }

    private async Task HandleAwaitingRfidForCustom(
        ITelegramBotClient botClient, Message message,
        Company? userCompany, long userId, string text,
        IEmployeesRepository employeesRepository, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
        {
            ClearUserPendingState(userId);
            return;
        }

        var rfidUid = NormaliseRfid(text);
        try
        {
            // Validate employee exists before proceeding with date prompts
            await employeesRepository.GetByRfidAsync(userCompany!.Id, rfidUid, ct);

            retryCounters.Remove(userId, out _);
            pendingCommands[userId] = $"awaitingFromDateEmployee:{rfidUid}";
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "🗓 Boshlanish sanasini kiriting (format: `dd.MM.yyyy`)\nMisol: `01.01.2025`",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
        catch
        {
            retryCounters.AddOrUpdate(userId, 1, (_, c) => c + 1);
            if (retryCounters.TryGetValue(userId, out var attempts) && attempts >= 3)
            {
                ClearUserPendingState(userId);
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "❌ 3 marta xato kiritdingiz. Buyruqni qaytadan boshlang.", cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: $"❌ Ushbu RFID bo'yicha xodim topilmadi: `{rfidUid}`. Qaytadan kiriting.",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
        }
    }

    private async Task HandleAwaitingFromDateEmployee(
        ITelegramBotClient botClient, Message message,
        long userId, string rfid, string text, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(text, "dd.MM.yyyy", out var fromDate))
        {
            retryCounters.AddOrUpdate(userId, 1, (_, c) => c + 1);
            if (retryCounters.TryGetValue(userId, out var attempts) && attempts >= 3)
            {
                ClearUserPendingState(userId);
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "❌ 3 marta xato kiritdingiz. Buyruqni qaytadan boshlang.", cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "⚠️ Noto'g'ri format. Iltimos `dd.MM.yyyy` formatida kiriting.\nMisol: `01.01.2025`",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            return;
        }

        retryCounters.Remove(userId, out _);
        pendingCommands[userId] = $"customReportEmployee:{rfid}:{fromDate:yyyy-MM-dd}";
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "🗓 Tugash sanasini kiriting (format: `dd.MM.yyyy`)\nMisol: `31.01.2025`",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandleCustomReportEmployee(
        ITelegramBotClient botClient, Message message,
        Company? userCompany, long userId, string pendingCmd, string text,
        IExcelReportService reportService, CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
        {
            ClearUserPendingState(userId);
            return;
        }

        // pendingCmd = "customReportEmployee:{rfid}:{yyyy-MM-dd}"
        var parts = pendingCmd.Split(':', 3);
        if (parts.Length < 3 || !DateOnly.TryParseExact(parts[2], "yyyy-MM-dd", out var fromDate))
        {
            ClearUserPendingState(userId);
            await botClient.SendMessage(chatId: message.Chat.Id, text: "⚠️ Ichki xatolik. Qayta urinib ko'ring.", cancellationToken: ct);
            return;
        }

        var rfid = parts[1];

        if (!DateOnly.TryParseExact(text, "dd.MM.yyyy", out var toDate))
        {
            retryCounters.AddOrUpdate(userId, 1, (_, c) => c + 1);
            if (retryCounters.TryGetValue(userId, out var attempts) && attempts >= 3)
            {
                ClearUserPendingState(userId);
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "❌ 3 marta xato kiritdingiz. Buyruqni qaytadan boshlang.", cancellationToken: ct);
            }
            else
            {
                await botClient.SendMessage(chatId: message.Chat.Id,
                    text: "⚠️ Noto'g'ri format. Iltimos `dd.MM.yyyy` formatida kiriting.\nMisol: `31.01.2025`",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            return;
        }

        ClearUserPendingState(userId);

        try
        {
            var (stream, fileName) = await reportService.GetEmployeeCustomRangeReportAsync(
                userCompany!.Id, rfid, fromDate, toDate, ct);
            await botClient.SendDocument(
                chatId: message.Chat.Id,
                document: new InputFileStream(stream, fileName),
                caption: $"🗓 {fromDate:dd.MM.yyyy} – {toDate:dd.MM.yyyy} davomat hisoboti",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await botClient.SendMessage(chatId: message.Chat.Id, text: $"⚠️ Xatolik: {ex.Message}", cancellationToken: ct);
        }
    }
}
