using System.Collections.Generic;
using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Entities;
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
                        text: "❌ You're not authorized for any company.",
                        cancellationToken: ct);
                    pendingCommands.Remove(userId, out _);
                    return;
                }

                var inputParts = message.Text.Split(',', 2);
                if (inputParts.Length != 2)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "⚠️ Please send the data in the format: `RFID_UID, Full Name`\nExample: `00 00 00 00, Eshmat Toshmatov`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    return;
                }

                var rfidUid = inputParts[0].Trim();
                var fullName = inputParts[1].Trim();

                try
                {
                    var updated = await employeesRepository.UpdateAsync(userCompany.Id, rfidUid, new Employee
                    {
                        Name = fullName
                    }, ct);

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"✅ Updated employee:\nRFID: `{updated.RFIDCardUID}`\nName: *{updated.Name}*",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"⚠️ Error: {ex.Message}",
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
                        text: "❌ You're not authorized for any company.",
                        cancellationToken: ct);
                    pendingCommands.Remove(userId, out _);
                    return;
                }

                try
                {
                    var rfidUid = message.Text.Trim();
                    var employee = await attendancesRepository.GetEmployeeByRfidAsync(userCompany.Id, rfidUid, ct);

                    if (employee == null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: $"❌ No employee found with RFID: `{rfidUid}`.",
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
                            text: $"✅ Checked *in* {employee.Name} (RFID: `{employee.RFIDCardUID}`)",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                    }
                    else
                    {
                        await attendancesRepository.CheckOutAsync(lastAttendance, ct);
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: $"✅ Checked *out* {employee.Name} (RFID: `{employee.RFIDCardUID}`)",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                    }
                }
                catch (Exception ex)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"⚠️ Error: {ex.Message}",
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
                    var welcomeText = userCompany is not null
                        ? $"👋 Welcome, {from.FirstName}! You're authorized for *{userCompany.Name}*."
                        : $"👋 Welcome, {from.FirstName}! You're not authorized for any company.";

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: welcomeText,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    break;

                case "/employees":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    var employees = await employeesRepository.GetAllAsync(userCompany.Id, ct);
                    var lines = employees.Select(e => $"• {e.Name} (RFID: `{e.RFIDCardUID}`)");
                    var messageText = "👥 Employees:\n" + string.Join("\n", lines);

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: messageText,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    return;
                // break;

                // change to /excelReportLastMonth
                case "/excelReport":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    var reportResult = await reportService.GetLastMonthReportAsync(userCompany.Id);

                    if (reportResult is null)
                    {
                        await reportService.GenerateMonthlyAttendanceReportsAsync(ct);
                        reportResult = await reportService.GetLastMonthReportAsync(userCompany.Id);

                        if (reportResult is null)
                        {
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: "⚠️ No data available to generate last month’s report.",
                                cancellationToken: ct);
                            return;
                        }
                    }

                    var fileStream = reportResult.FileStream;
                    var fileName = reportResult.FileDownloadName;

                    await botClient.SendDocument(
                        chatId: message.Chat.Id,
                        document: new InputFileStream(fileStream, fileName),
                        caption: $"📊 Attendance Report for {userCompany.Name}",
                        cancellationToken: ct);

                    break;

                case "/15daysReport":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    var report15 = await reportService.Get15DayReportAsync(userCompany.Id);

                    if (report15 is null)
                    {
                        await reportService.Generate15DayAttendanceReportsAsync(ct);
                        report15 = await reportService.Get15DayReportAsync(userCompany.Id);

                        if (report15 is null)
                        {
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: "⚠️ No data available to generate the 15-day report.",
                                cancellationToken: ct);
                            return;
                        }
                    }

                    await botClient.SendDocument(
                        chatId: message.Chat.Id,
                        document: new InputFileStream(report15.FileStream, report15.FileDownloadName),
                        caption: $"📆 15-Day Attendance Report for {userCompany.Name}",
                        cancellationToken: ct);
                    break;

                case "/newAttendance":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    pendingCommands[userId] = "newAttendance";

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "📮 Please send the RFID UID of the employee.",
                        cancellationToken: ct);
                    break;

                case "/updateEmployee":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    pendingCommands[userId] = "updateEmployee";

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "✏️ Please send the employee's *RFID UID and full name*, separated by a comma.\n\nExample:\n`00 00 00 00, Eshmat Toshmatov`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    break;

                case "/checkedIn":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    var checkedInEmployees = await attendancesRepository.GetAllCheckInAsync(userCompany.Id, ct);
                    if (!checkedInEmployees.Any())
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "ℹ️ No employees are currently checked in.",
                            cancellationToken: ct);
                    }
                    else
                    {
                        var inLines = checkedInEmployees
                            .Select(a => $"• {a!.Employee!.Name} (RFID: `{a.Employee.RFIDCardUID}`) at {a.CheckIn:HH:mm:ss}");
                        var checkedInText = "✅ *Currently Checked-In Employees:*\n" + string.Join("\n", inLines);

                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: checkedInText,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                    }
                    break;

                case "/checkedOut":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ You're not authorized for any company.",
                            cancellationToken: ct);
                        return;
                    }

                    var checkedOutEmployees = await attendancesRepository.GetAllCheckOutAsync(userCompany.Id, ct);
                    if (!checkedOutEmployees.Any())
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "ℹ️ No employees have checked out today.",
                            cancellationToken: ct);
                    }
                    else
                    {
                        var outLines = checkedOutEmployees
                            .Select(a => $"• {a!.Employee!.Name} (RFID: `{a.Employee.RFIDCardUID}`) at {a.CheckOut:HH:mm:ss}");
                        var checkedOutText = "🏁 *Checked-Out Employees:*\n" + string.Join("\n", outLines);

                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: checkedOutText,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                    }
                    break;

                default:
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Unknown command: " + message.Text,
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
            text: "Unknown message type (type of the message): " + message.Type,
            cancellationToken: cancellationToken);
    }
}