using System.Collections.Generic;
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

            if (pendingCommands.TryGetValue(userId, out var pendingCmd) && pendingCmd == "updateEmployee")
            {
                if (userCompany is null)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                        cancellationToken: ct);
                    pendingCommands.Remove(userId, out _);
                    return;
                }

                var inputParts = message.Text.Split(',', 2);
                if (inputParts.Length != 2)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "⚠️ Maʼlumotni quyidagi formatda yuboring: `RFID_UID, To‘liq ism`\nMisol: `00 00 00 00, Eshmat Toshmatov`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    return;
                }

                // var rfidUid = inputParts[0].Trim().Replace(" ", "").ToUpperInvariant();
                var rfidUid = inputParts[0].Trim().ToUpperInvariant();
                var fullName = inputParts[1].Trim();

                try
                {
                    var updated = await employeesRepository.UpdateAsync(userCompany.Id, rfidUid, new Employee
                    {
                        Name = fullName
                    }, ct);

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"✅ Yangilangan xodim:\nRFID: `{updated.RFIDCardUID}`\nIsm: *{updated.Name}*",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"⚠️ Xatolik: {ex.Message}",
                        cancellationToken: ct);
                }

                pendingCommands.Remove(userId, out _);
                return;
            }
            if (pendingCommands.TryGetValue(userId, out var pending) && pending == "newAttendance")
            {
                if (userCompany is null)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                        cancellationToken: ct);
                    pendingCommands.Remove(userId, out _);
                    return;
                }

                try
                {
                    var rfidUid = message.Text.Trim().Replace(" ", "").ToUpperInvariant();
                    var employee = await attendancesRepository.GetEmployeeByRfidAsync(userCompany.Id, rfidUid, ct);

                    if (employee == null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: $"❌ Ushbu RFID bo‘yicha xodim topilmadi: `{rfidUid}`.",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                        pendingCommands.Remove(userId, out _);
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
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"⚠️ Xatolik: {ex.Message}",
                        cancellationToken: ct);
                }

                // Clear the pending command
                pendingCommands.Remove(userId, out _);
                return;
            }

            // Handle regular commands
            switch (message.Text.Trim())
            {
                case "/start":
                    await HandleStartCommand(botClient, message, userCompany, ct);
                    break;

                case "/employees":
                    await HandleEmployeesCommand(botClient, message, userCompany, employeesRepository, ct);
                    // return;
                    break;

                // change to /excelReportLastMonth
                case "/excel_report":
                    await HandleExcelReportCommand(botClient, message, userCompany, reportService, ct);
                    break;

                case "/15daysreport":
                    await Handle15DaysReportCommand(botClient, message, userCompany, reportService, ct);
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

                // Mind this command
                // case "/cancel":
                //     pendingCommands.Remove(userId, out _);
                //     await botClient.SendMessage(
                //         chatId: message.Chat.Id,
                //         text: "🚫 Amal bekor qilindi.",
                //         cancellationToken: ct);
                //     break;

                default:
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Nomaʼlum buyruq: " + message.Text,
                        cancellationToken: ct);
                    break;
            }

        }, cancellationToken);
    }


    private static async Task HandleUnknownMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var from = message.From;

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Nomaʼlum xabar turi: " + message.Type,
            cancellationToken: cancellationToken);
    }
}