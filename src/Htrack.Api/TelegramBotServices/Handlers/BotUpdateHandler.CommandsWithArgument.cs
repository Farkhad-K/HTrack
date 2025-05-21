using HTrack.Api.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HTrack.Api.TelegramBotServices;

public partial class BotUpdateHandler
{
    private async Task HandleNewAttendanceCommand(
        ITelegramBotClient botClient,
        Message message,
        Company? userCompany,
        long userId,
        CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        pendingCommands[userId] = "newAttendance";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "📮 Iltimos, xodimning RFID UID kodini yuboring.",
            cancellationToken: ct);
    }

    private async Task HandleUpdateEmployeeCommand(
        ITelegramBotClient botClient,
        Message message,
        Company? userCompany,
        long userId,
        CancellationToken ct)
    {
        if (!await EnsureCompanyAccess(botClient, message, userCompany, ct))
            return;

        pendingCommands[userId] = "updateEmployee";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "✏️ Iltimos, xodimning *RFID UID va to‘liq ismini* vergul bilan ajratib yuboring.\n\nMisol:\n`00 00 00 00, Eshmat Toshmatov`",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }
}