using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot
{
    public class CommandsHandler
    {
        private ITelegramBotClient botClient;

        public readonly Dictionary<string, Func<long, Task>> commands;

        public CommandsHandler(ITelegramBotClient telegramBotClient)
        {
            this.botClient = telegramBotClient;

            commands = new Dictionary<string, Func<long, Task>>
            {
                { "start", StartCommand},
                 { "gaysex", TestFunc }
            };
        }
        public async Task SetCommands()
        {
            var commandsToDisplay = new[]
            {
                new BotCommand {Command = "gaysex", Description = "ну а хули" }
            };

            await botClient.SetMyCommandsAsync(commandsToDisplay);
        }
        private async Task TestFunc(long chatId)
        {
            await botClient.SendTextMessageAsync(chatId, "Testfunc was called!");
        }
        private async Task StartCommand(long chatId)
        {
            await botClient.SendTextMessageAsync(chatId, "Welcome! Use /help to see available commands.");
        }
    }
}
