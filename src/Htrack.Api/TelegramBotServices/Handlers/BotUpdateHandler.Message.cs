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
                "ℹ️ Yuqoridagi buyruqlar yordamida kompaniyangizning tashrif tizimi bilan samarali ishlang.";

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
                    var rfidUid = message.Text.Trim();
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
                    var welcomeText = userCompany is not null
                        ? $"👋 Assalomu alaykum, {from.FirstName}! Siz *{userCompany.Name}* kompaniyasiga ruxsatga egasiz.\n\n{greeting_HelpMsg}"
                        : $"👋 Assalomu alaykum, {from.FirstName}! Siz hech qanday kompaniyaga ruxsatga ega emassiz.";

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
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                            cancellationToken: ct);
                        return;
                    }

                    var employees = await employeesRepository.GetAllAsync(userCompany.Id, ct);
                    var lines = employees.Select(e => $"• {e.Name} (RFID: `{e.RFIDCardUID}`)");
                    var messageText = "👥 Xodimlar ro‘yxati:\n" + string.Join("\n", lines);

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: messageText,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    return;
                // break;

                // change to /excelReportLastMonth
                case "/excel_report":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
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

                    break;

                case "/15daysreport":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(chatId: message.Chat.Id,
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
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
                    break;

                case "/new_attendance":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                            cancellationToken: ct);
                        return;
                    }

                    pendingCommands[userId] = "newAttendance";

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "📮 Iltimos, xodimning RFID UID kodini yuboring.",
                        cancellationToken: ct);
                    break;

                case "/update_employee":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                            cancellationToken: ct);
                        return;
                    }

                    pendingCommands[userId] = "updateEmployee";

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "✏️ Iltimos, xodimning *RFID UID va to‘liq ismini* vergul bilan ajratib yuboring.\n\nMisol:\n`00 00 00 00, Eshmat Toshmatov`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    break;

                case "/checked_in":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                            cancellationToken: ct);
                        return;
                    }

                    var checkedInEmployees = await attendancesRepository.GetAllCheckInAsync(userCompany.Id, ct);
                    if (!checkedInEmployees.Any())
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "ℹ️ Hozirda hech bir xodim ishda emas.",
                            cancellationToken: ct);
                    }
                    else
                    {
                        var inLines = checkedInEmployees
                            .Select(a => $"• {a!.Employee!.Name} (RFID: `{a.Employee.RFIDCardUID}`) at {a.CheckIn:HH:mm:ss}");
                        var checkedInText = "✅ *Hozirda ishda bo‘lgan xodimlar:*\n" + string.Join("\n", inLines);

                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: checkedInText,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                    }
                    break;

                case "/checked_out":
                    if (userCompany is null)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: "❌ Siz hech qanday kompaniya uchun ruxsatga ega emassiz.",
                            cancellationToken: ct);
                        return;
                    }

                    var checkedOutEmployees = await attendancesRepository.GetAllCheckOutAsync(userCompany.Id, ct);
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
                            .Select(a => $"• {a!.Employee!.Name} (RFID: `{a.Employee.RFIDCardUID}`) at {a.CheckOut:HH:mm:ss}");
                        var checkedOutText = "🏁 *Bugun ishni tugatgan xodimlar:*\n" + string.Join("\n", outLines);

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