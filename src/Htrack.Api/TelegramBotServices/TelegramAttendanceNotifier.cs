using HTrack.Api.Abstractions;
using HTrack.Api.Entities;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using HTrack.Api.Utilities;

namespace HTrack.Api.TelegramBotServices;

public class TelegramAttendanceNotifier(ITelegramBotClient botClient) : IAttendanceNotifier
{
    public async Task NotifyAttendanceAsync(Employee employee, Attendance attendance, bool isCheckIn, CancellationToken cancellationToken = default)
    {
        var chatId = employee.Company!.TgChatID;
        var status = isCheckIn ? "🟢 Ishga keldi" : "🔴 Ishdan chiqdi";
        var timeUtc = isCheckIn ? attendance.CheckIn : attendance.CheckOut;
        var timeUz = TimeHelper.ToUzbekistanTime(timeUtc ?? DateTime.UtcNow);

        var message = $"{status} - {employee.Name} soat {timeUz:HH:mm:ss} da";

        await botClient.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
