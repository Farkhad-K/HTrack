using System.Collections.Concurrent;
using System.Reflection.Metadata;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HTrack.Api.TelegramBotServices;

public partial class BotUpdateHandler(
    ILogger<BotUpdateHandler> logger,
    IServiceScopeFactory scopeFactory) : IUpdateHandler
{
    private readonly ConcurrentDictionary<long, string> pendingCommands = new();

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        // logger.LogError(exception, "Error in bot handler: {Source}", source);
        logger.LogInformation("Error occured with Telegram bot: {e.Message}", exception);

        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {

        var handler = update.Type switch
        {
            UpdateType.Message => HandleMessage(botClient, update.Message, cancellationToken),
            // Could be added more for other update types
            _ => HandleUnknownUpdate(botClient, update, cancellationToken)
        };

        try
        {
            await handler;
        }
        catch (Exception e)
        {
            await HandleErrorAsync(botClient, e, HandleErrorSource.PollingError, cancellationToken);
        }
    }

    private Task HandleUnknownUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unknown update type: {Type}", update.Type);
        return Task.CompletedTask;
    }

    private async Task UseScopedServiceAsync(
        Func<IServiceProvider, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        await action(scope.ServiceProvider, cancellationToken);
    }
}