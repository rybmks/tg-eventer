using bot.Interfaces;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot
{
    public static class CommandsHandler
    {
        static ITelegramBotClient botClient;
        private static IDbManager db;
        private static ILogger logger;
        public static Dictionary<string, Func<Message, Task>> commands;
        
        static CommandsHandler()
        {
            if (BotServicesContainer.BotClient == null)
            {
                throw new ArgumentNullException(nameof(BotServicesContainer.BotClient), "BotClient cannot be null");
            }

            logger = BotServicesContainer.Logger;
            botClient = BotServicesContainer.BotClient;
            db = new DbManager(BotServicesContainer.ConnectionString);

            commands = new Dictionary<string, Func<Message, Task>>
            {
                {"start", StartCommand},
                {"help", HelpMessage },
                {"events", ShowAllEvents },
                {"add", CreateEvent },
                {"remove", RemoveEvent},
                {"ping", Ping },
                {"пасхалка", Pashalka },
                {"пиво", Beer }
            };
        }
        public static async Task ShowCommands()
        {
            var commandsToDisplay = new[]
            {
                new BotCommand {Command = "help", Description = "Show available commands" },
                new BotCommand {Command = "events", Description = "Show all created events"},
                new BotCommand {Command = "add", Description = "Create by: /add *event-name*"},
                new BotCommand {Command = "ping", Description = "Ping all subscribed users: /ping *event-name*"},
                new BotCommand {Command = "remove", Description = "/remove *event-name*"},
            };
            await botClient.SetMyCommandsAsync(commandsToDisplay);
        }
        private static async Task Ping(Message message) 
        {
            if (message == null || message.Text == null)
                return;

            (string _, string eventName) = NormalizeCommand(message.Text);

            List<string> usernames = await db.Ping(message, eventName);

            if (usernames.Count < 1) 
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "There are no subs for this event");
            }
            else 
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("ПАДЙОМ:");

                foreach (var user in usernames)
                {
                    sb.Append($" @{user},");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append('!');
                await botClient.SendTextMessageAsync(message.Chat.Id, sb.ToString());
            }
        }
        private static async Task RemoveEvent(Message message)
        {
            if (message == null || message.Text == null)
                return;

            (string _, string eventName) = NormalizeCommand(message.Text);

            ExecutionStatus code = await db.DeleteEvent(message, eventName);

            string mes;

            switch (code)
            {
                case ExecutionStatus.Success:
                    mes = "Event deleted successfully";
                    break;
                case ExecutionStatus.EventDoesNotExistsError:
                    mes = "Event does not exists!";
                    break;
                case ExecutionStatus.InsufficientRightsError:
                    mes = "Only admins and authors of event are allowed to delete an event!";
                    break;
                default:
                    mes = "Some error!";
                    break;
            }

            await botClient.SendTextMessageAsync(
              chatId: message.Chat.Id,
              text: mes,
              replyToMessageId: message.MessageId
              );
        }
        private static async Task<bool> CheckUsername(Message message)
        {
            User? user = message.From;

            if (user?.Username == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "You shold have an username for using bot",
                    replyToMessageId: message.MessageId
                    );

                return false;
            }
            else
                return true;
        }
        private static async Task CreateEvent(Message message)
        {
            var user = message.From;

            if (!await CheckUsername(message))
                return;

            if (message.Text == null)
            {
                logger.Error("Error in create event method. Message is empty");
                return;
            }

            (string _, string nameOfEvent) = NormalizeCommand(message.Text);

            if (nameOfEvent == null || nameOfEvent.Length <= 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter name of event");
                return;
            }
            else if (nameOfEvent.Length > 25)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Length of event name must be less than 25 chars. Try again.");
                return;
            }

            if (user == null)
            {
                BotServicesContainer.Logger.Error("User is null");
                return;
            }

            ExecutionStatus code = await db.CreateEvent(message, nameOfEvent);

            string mes;
            switch (code)
            {
                case ExecutionStatus.Success:
                    mes = "Event was created";
                    break;

                case ExecutionStatus.EventAlreadyExistsError:
                    mes = "Event already exists!";
                    break;

                default:
                    mes = "Some error!";
                    break;
            }

            await botClient.SendTextMessageAsync(
                      chatId: message.Chat.Id,
                      text: mes,
                      replyToMessageId: message.MessageId);
            
            await Task.Delay(100);
            await ShowAllEvents(message);
        }
        private static async Task ShowAllEvents(Message message)
        {
            string[] events = await db.GetEventsAsync(message.Chat.Id);

            if (events.Length == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "There are no created event for this chat");
                return;
            }

            var keyboardButtons = new List<List<InlineKeyboardButton>>();

            const int itemsPerLine = 3;

            for (int i = 0; i < events.Length; i += itemsPerLine)
            {
                var keyboardLine = new List<InlineKeyboardButton>();

                for (int j = i; j < i + itemsPerLine && j < events.Length; j++)
                {
                    var button = InlineKeyboardButton.WithCallbackData(events[j], $"{events[j]}");
                    keyboardLine.Add(button);
                }

                keyboardButtons.Add(keyboardLine);
            }

            var unsubLine = new List<InlineKeyboardButton>();

            var bUnsub = InlineKeyboardButton.WithCallbackData("Unsubscribe all", "command_unsub");
            unsubLine.Add(bUnsub);
            keyboardButtons.Add(unsubLine);
            var bShowMySubs = InlineKeyboardButton.WithCallbackData("Show my subs", "command_mysubs");
            var showMyLine = new List<InlineKeyboardButton>();
            showMyLine.Add(bShowMySubs);
            keyboardButtons.Add(showMyLine);
            var inlineKeyboard = new InlineKeyboardMarkup(keyboardButtons);


            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "All events: ",
                replyMarkup: inlineKeyboard
            );
        }
        private static async Task HelpMessage(Message message) =>
            await botClient.SendTextMessageAsync(message.Chat.Id,
                @"Hello I'm tg_eventer_bot! I was created due to 
beer abuse, but it doesn't make me useless. I can
notify people, who subscribed to some 
action (I call it an event) by using 
'/ping *name-of-event*' command. Your task is 
creating new events by using '/create *event-name*'
command and give chat members an opportunity 
to subscribe to this event by pressing a button,
which you can see after using the '/events' command. 
Due to this awesome magic, nobody will skip 
your beer-drinking party on playground.");
        private static async Task StartCommand(Message message) =>
            await botClient.SendTextMessageAsync(message.Chat.Id, "Welcome! Use /help to see available commands.");
        private static async Task Pashalka(Message message) =>
            await botClient.SendTextMessageAsync(message.Chat.Id, @"
⡶⠶⠂⠐⠲⠶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⣶⡶⠶⡶
⣗⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⠿⣿⠿⣿⣿⣿⣿⠿⠿⠿⠟⠛⠉⠁⠀⠀⠀⢠⣿
⣿⣷⣀⠀⠈⠛⠢⠥⠴⠟⠂⠀⠀⠀⠉⣛⠉⠁⠀⠐⠲⠤⠖⠛⠁⠀⠀⣐⣿⣿
⣿⣿⣿⣦⣄⡀⠀⠀⠀⠀⣀⡠⣤⣦⣿⣿⣿⣆⣴⣠⣀⣀⡀⣀⣀⣚⣿⣿⣿⢳
⣧⠉⠙⢿⣿⣿⣶⣶⣾⣿⡿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠿⢇⣿
⣿⣷⡄⠈⣿⣿⣿⣿⣯⣥⣦⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡟⢉⣴⣿⣿
⣿⣿⣿⣦⣘⠋⢻⠿⢿⣿⣿⣿⣾⣭⣛⣛⣛⣯⣷⣿⣿⠿⠟⠋⠉⣴⣿⣿⣿⣿
⢠⠖⢲⠀⠀⡖⢲⡄⡴⠒⠆⡖⠒⠂⠀⣶⠲⡄⢰⡆⠀⡖⢦⠀⡆⢰⡆⡴⠒⣄
⢨⠟⢻⠀⠀⣏⣉⠇⢧⣀⡄⣏⣉⡁⠀⣿⠚⢡⠗⠺⡄⣏⣹⠆⡏⢹⡇⢧⣀⡞
⢰⣒⡒⠰⡄⡴⠀⡶⢲⡆⢢⣀⡖⠀⠀⡖⠒⠲⢰⠒⣦⢀⡶⡄⠒⢲⠒⢲⣆⣀
⠸⠤⠽⠠⠽⠁⣴⠧⠼⣧⠤⠟⠀⠀⠈⠧⣤⠤⠸⠉⠁⠞⠒⠳⠀⠸⠀⠸⠧⠼");
        private static async Task Beer(Message message) 
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                @"🍺🍺🍺🍺🍺🍺🍺🍺🍺");
        }
        private static (string, string) NormalizeCommand(string text)
        {
            const string botUsername = "@TrempelsEventer_bot";
            string command;
            string additionalText = string.Empty;

            if (!text.Contains(botUsername))
            {
                var spl = text.Split();
                command = spl[0].Trim('/');

                if (spl.Length > 1)
                    additionalText = string.Join(" ", spl.Skip(1));
            }
            else
            {
                int botUsernameBegin = text.IndexOf('@');
                int botUsernameLength = botUsername.Length;

                command = text.Substring(1, botUsernameBegin - 1);

                if (text.Length > botUsernameBegin + botUsernameLength + 1)
                    additionalText = text.Substring(botUsernameBegin + botUsernameLength + 1);
            }

            return (command, additionalText.Trim());
        }
    }
}
