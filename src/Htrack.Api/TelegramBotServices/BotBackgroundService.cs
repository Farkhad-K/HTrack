
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace HTrack.Api.TelegramBotServices;

public class BotBackgroundService(
    ILogger<BotBackgroundService> logger,
    ITelegramBotClient client,
    IUpdateHandler handler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = await client.GetMe(stoppingToken);
        logger.LogInformation("Bot started successfully. Username: {bot.Username}", bot.Username);

        client.StartReceiving(
            handler.HandleUpdateAsync,
            handler.HandleErrorAsync,
            new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            }, stoppingToken);
    }
}