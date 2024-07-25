using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot
{
    public static class CommandsHandler
    {
        static ITelegramBotClient botClient;

        public static Dictionary<string, Func<Message, Task>> commands;

        static CommandsHandler()
        {
            if (BotServicesContainer.BotClient == null)
            {
                throw new ArgumentNullException(nameof(BotServicesContainer.BotClient), "BotClient cannot be null");
            }

            botClient = BotServicesContainer.BotClient;
            commands = new Dictionary<string, Func<Message, Task>>
            {
                { "start", StartCommand},
                { "help", HelpMessage },
                { "events", ShowAllEvents },
                {"сreate", CreateEvent }
            };
        }
        public static async Task ShowCommands()
        {
            var commandsToDisplay = new[]
            {
                new BotCommand {Command = "help", Description = "Show available commands" },
                new BotCommand {Command = "events", Description = "Show all created events"},
                new BotCommand {Command = "сreate", Description = "Creating a new event. Create by: /create *event-name*"},
                new BotCommand {Command = "remove", Description = "Remove an event. To remove: /create *event-name*"},
            };
            await botClient.SetMyCommandsAsync(commandsToDisplay);
        }

        private static async Task CreateEvent(Message message)
        {
            throw new NotImplementedException();
        }

        private static async Task ShowAllEvents(Message message)
        {
            var events = TempEntities.GetEvents();

            var keyboardButtons = new List<List<InlineKeyboardButton>>();

            const int itemsPerLine = 3;

            for (int i = 0; i < events.Length; i += itemsPerLine)
            {
                var keyboardLine = new List<InlineKeyboardButton>();

                for (int j = i; j < i + itemsPerLine && j < events.Length; j++)
                {
                    var button = InlineKeyboardButton.WithCallbackData(events[j], $"event_{events[j]}");
                    keyboardLine.Add(button);
                }

                keyboardButtons.Add(keyboardLine);
            }

            var inlineKeyboard = new InlineKeyboardMarkup(keyboardButtons);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "All events: ",
                replyMarkup: inlineKeyboard
            );
        }

        private static async Task HelpMessage(Message message) =>
            await botClient.SendTextMessageAsync(message.Chat.Id, "Testfunc was called!");

        private static async Task StartCommand(Message message) =>
            await botClient.SendTextMessageAsync(message.Chat.Id, "Welcome! Use /help to see available commands.");
    }
}
