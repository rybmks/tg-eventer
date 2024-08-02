using bot.Interfaces;
using System;
using Telegram.Bot;

namespace bot
{
    public static class BotServicesContainer
    {
        private static string? _connectionString;
        private static ITelegramBotClient? _botClient;

        public static string ConnectionString
        {
            get => _connectionString ?? throw new InvalidOperationException("Connection string is not initialized.");
            set => _connectionString = value;
        }
        public static ITelegramBotClient BotClient
        {
            get => _botClient ?? throw new InvalidOperationException("BotClient is not initialized.");
            set => _botClient = value;
        }
        public static ILogger Logger { get; } = new ConsoleLogger();
        public static DateTime StartedAt { get; set; } = DateTime.MinValue;
    }
}
