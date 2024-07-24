using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot
{
    public class ComandsHandler
    {
        private ITelegramBotClient botClient;
        public ComandsHandler(ITelegramBotClient telegramBotClient)
        {
            this.botClient = telegramBotClient;
        }

        public static readonly Dictionary<BotCommand, Func<long, Task>> commands = new Dictionary<BotCommand, Func<long, Task>>
            {
                {new BotCommand {Command = "start", Description = "Starting bot" }, StartCommand},
            };

        public async Task SetCommands()
        {
            await botClient.SetMyCommandsAsync(commands.Keys);
        }
        
        private static async Task StartCommand(long chatId)
        {
            await Program.botClient.SendTextMessageAsync(chatId, "Welcome! Use /help to see available commands.");
        }
    }
}
