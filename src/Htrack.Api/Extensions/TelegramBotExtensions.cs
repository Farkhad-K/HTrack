using HTrack.Api.Abstractions;
using HTrack.Api.TelegramBotServices;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace HTrack.Api.Extensions;

internal static class TelegramBotExtensions
{
    internal static IServiceCollection AddTelegramBot(
        this IServiceCollection services, IConfiguration configuration)
    {
        var botToken = configuration["TelegramBot:Token"]!;

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
        services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
        services.AddScoped<IAttendanceNotifier, TelegramAttendanceNotifier>();
        services.AddHostedService<BotBackgroundService>();

        return services;
    }
}
