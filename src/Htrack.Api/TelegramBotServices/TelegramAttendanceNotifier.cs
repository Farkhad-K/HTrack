using HTrack.Api.Abstractions;
using HTrack.Api.Entities;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HTrack.Api.TelegramBotServices;

public class TelegramAttendanceNotifier(
    ITelegramBotClient botClient) : IAttendanceNotifier
{
    public async Task NotifyAttendanceAsync(Employee employee, Attendance attendance, bool isCheckIn, CancellationToken cancellationToken = default)
    {
        var chatId = employee.Company!.TgChatID;
        var status = isCheckIn ? "🟢 Check-In" : "🔴 Check-Out";
        var time = isCheckIn ? attendance.CheckIn : attendance.CheckOut;

        var message = $"{status} - {employee.Name} at {time?.ToLocalTime():HH:mm:ss}";

        await botClient.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
