using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Entities;
using HTrack.Api.Utilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HTrack.Api.TelegramBotServices;

public partial class BotUpdateHandler
{
    private async Task HandleMessage(ITelegramBotClient botClient, Message? message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var from = message.From;
        var ms = message.Text;
        logger.LogInformation("Received message: \"{ms}\" from {from.Firstname} with user id {from.Id}", ms, from?.FirstName, from?.Id);

        var handler = message.Type switch
        {
            MessageType.Text => HandleTextMessageAsync(botClient, message, cancellationToken),
            _ => HandleUnknownMessageAsync(botClient, message, cancellationToken)
        };

        await handler;
    }

    private static async Task<bool> EnsureCompanyAccess(
        ITelegramBotClient botClient,
        Message message,
        Company? userCompany,
        CancellationToken ct)
    {
        if (userCompany is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                cancellationToken: ct);
            return false;
        }

        return true;
    }

    private async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var from = message.From;
        if (from is null || string.IsNullOrWhiteSpace(message.Text))
            return;

        await UseScopedServiceAsync(async (services, ct) =>
        {
            var companiesRepository = services.GetRequiredService<ICompaniesRepository>();
            var employeesRepository = services.GetRequiredService<IEmployeesRepository>();
            var attendancesRepository = services.GetRequiredService<IAttendancesRepository>();
            var reportService = services.GetRequiredService<IExcelReportService>();

            var companies = await companiesRepository.GetAllAsync(ct);
            var userCompany = companies.FirstOrDefault(c => c.ManagerTgUserIDs.Contains(from.Id));

            var userId = from.Id;
            var text = message.Text.Trim();

            // --- Pending state handlers ---

            if (pendingCommands.TryGetValue(userId, out var pendingCmd))
            {
                // Any known command/button while in a pending state cancels it
                var mappedCmd = MapButtonToCommand(text);
                if (mappedCmd.StartsWith('/'))
                {
                    ClearUserPendingState(userId);
                    await botClient.SendMessage(chatId: message.Chat.Id,
                        text: "↩️ Avvalgi amal bekor qilindi.", cancellationToken: ct);
                    // fall through to command routing below
                }
                else
                {
                    if (pendingCmd == "updateEmployee")
                    {
                        await HandleUpdateEmployeePending(botClient, message, userCompany, userId, text, employeesRepository, ct);
                        return;
                    }

                    if (pendingCmd == "newAttendance")
                    {
                        await HandleNewAttendancePending(botClient, message, userCompany, userId, text, attendancesRepository, ct);
                        return;
                    }

                    if (pendingCmd == "awaitingRfidFor15Day")
                    {
                        await HandleAwaitingRfidFor15Day(botClient, message, userCompany, userId, text, reportService, ct);
                        return;
                    }

                    if (pendingCmd == "awaitingRfidForMonthToDate")
                    {
                        await HandleAwaitingRfidForMonthToDate(botClient, message, userCompany, userId, text, reportService, ct);
                        return;
                    }

                    if (pendingCmd == "awaitingRfidForCustom")
                    {
                        await HandleAwaitingRfidForCustom(botClient, message, userCompany, userId, text, employeesRepository, ct);
                        return;
                    }

                    if (pendingCmd.StartsWith("awaitingFromDateEmployee:"))
                    {
                        var rfid = pendingCmd["awaitingFromDateEmployee:".Length..];
                        await HandleAwaitingFromDateEmployee(botClient, message, userId, rfid, text, ct);
                        return;
                    }

                    if (pendingCmd.StartsWith("customReportEmployee:"))
                    {
                        await HandleCustomReportEmployee(botClient, message, userCompany, userId, pendingCmd, text, reportService, ct);
                        return;
                    }

                    return; // unknown pending state
                }
            }

            // --- Command / button switch ---

            var command = MapButtonToCommand(text);

            switch (command)
            {
                case "/start":
                    await HandleStartCommand(botClient, message, userCompany, ct);
                    break;

                case "/cancel":
                    await HandleCancelCommand(botClient, message, userId, ct);
                    break;

                case "/employees":
                    await HandleEmployeesCommand(botClient, message, userCompany, employeesRepository, ct);
                    break;

                case "/excel_report":
                    await HandleExcelReportCommand(botClient, message, userCompany, reportService, ct);
                    break;

                case "/15daysreport":
                    await Handle15DaysReportCommand(botClient, message, userCompany, userId, ct);
                    break;

                case "/employee_monthly":
                    await HandleEmployeeMonthlyCommand(botClient, message, userCompany, userId, ct);
                    break;

                case "/report_till_today":
                    await HandleReportTillTodayCommand(botClient, message, userCompany, reportService, ct);
                    break;

                case "/custom_report":
                    await HandleCustomReportCommand(botClient, message, userCompany, userId, ct);
                    break;

                case "/new_attendance":
                    await HandleNewAttendanceCommand(botClient, message, userCompany, userId, ct);
                    break;

                case "/update_employee":
                    await HandleUpdateEmployeeCommand(botClient, message, userCompany, userId, ct);
                    break;

                case "/checked_in":
                    await HandleCheckedInCommand(botClient, message, userCompany, attendancesRepository, ct);
                    break;

                case "/checked_out":
                    await HandleCheckedOutCommand(botClient, message, userCompany, attendancesRepository, ct);
                    break;

                default:
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Nomaʼlum buyruq: " + message.Text,
                        cancellationToken: ct);
                    break;
            }

        }, cancellationToken);
    }

    private static string MapButtonToCommand(string text) => text switch
    {
        "👥 Xodimlar"       => "/employees",
        "✅ Ishda"          => "/checked_in",
        "🚪 Ishdan chiqdi"  => "/checked_out",
        "📊 O'tgan oy"      => "/excel_report",
        "📅 15 kunlik"      => "/15daysreport",
        "📆 Bugunga"        => "/report_till_today",
        "🗓 Ixtiyoriy sana" => "/custom_report",
        "📋 Xodim oylik"    => "/employee_monthly",
        "✏️ Davomat"        => "/new_attendance",
        "🔄 Yangilash"      => "/update_employee",
        _ => text
    };

    private async Task HandleUpdateEmployeePending(
        ITelegramBotClient botClient, Message message, Company? userCompany,
        long userId, string text, IEmployeesRepository employeesRepository, CancellationToken ct)
    {
        if (userCompany is null)
        {
            await botClient.SendMessage(chatId: message.Chat.Id, text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.", cancellationToken: ct);
            ClearUserPendingState(userId);
            return;
        }

        var inputParts = text.Split(',', 2);
        if (inputParts.Length != 2)
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
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⚠️ Maʼlumotni quyidagi formatda yuboring: `RFID_UID, To'liq ism`\nMisol: `00 00 00 00, Eshmat Toshmatov`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
            }
            return;
        }

        var rfidUid = inputParts[0].Trim().ToUpperInvariant();
        var fullName = inputParts[1].Trim();

        try
        {
            var updated = await employeesRepository.UpdateAsync(userCompany.Id, rfidUid, new Employee { Name = fullName }, ct);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"✅ Yangilangan xodim:\nRFID: `{updated.RFIDCardUID}`\nIsm: *{updated.Name}*",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await botClient.SendMessage(chatId: message.Chat.Id, text: $"⚠️ Xatolik: {ex.Message}", cancellationToken: ct);
        }

        ClearUserPendingState(userId);
    }

    private async Task HandleNewAttendancePending(
        ITelegramBotClient botClient, Message message, Company? userCompany,
        long userId, string text, IAttendancesRepository attendancesRepository, CancellationToken ct)
    {
        if (userCompany is null)
        {
            await botClient.SendMessage(chatId: message.Chat.Id, text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.", cancellationToken: ct);
            ClearUserPendingState(userId);
            return;
        }

        try
        {
            var rfidUid = text.Replace(" ", "").ToUpperInvariant();
            var employee = await attendancesRepository.GetEmployeeByRfidAsync(userCompany.Id, rfidUid, ct);

            if (employee == null)
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
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"❌ Ushbu RFID bo'yicha xodim topilmadi: `{rfidUid}`.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                }
                return;
            }

            var lastAttendance = await attendancesRepository.GetLastAttendanceAsync(employee.Id, ct);

            if (lastAttendance is null || lastAttendance.CheckOut != null)
            {
                await attendancesRepository.CheckInAsync(employee.Id, ct);
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"✅ *{employee.Name}* ishga keldi (RFID: `{employee.RFIDCardUID}`)",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
            }
            else
            {
                await attendancesRepository.CheckOutAsync(lastAttendance, ct);
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"✅ *{employee.Name}* ishni tugatdi (RFID: `{employee.RFIDCardUID}`)",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            await botClient.SendMessage(chatId: message.Chat.Id, text: $"⚠️ Xatolik: {ex.Message}", cancellationToken: ct);
        }

        ClearUserPendingState(userId);
    }

    private static async Task HandleUnknownMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Nomaʼlum xabar turi: " + message.Type,
            cancellationToken: cancellationToken);
    }
}
