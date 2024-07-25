using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace bot
{
    public static class BotUpdates
    {
        public static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            Console.WriteLine(update.Type);

            if (update.CallbackQuery != null)
            {
                await CallbackHandler.HandleCallback(botClient, update, token);
            }
            if (update.Message is Message message && message.Text != null && message.Text.StartsWith('/'))
            {
                var commandText = message.Text.Trim('/');
                var commands = CommandsHandler.commands;

                if (commands != null && commands.ContainsKey(commandText))
                {
                    await commands[commandText](message);
                }
            }
            else if (update?.Type is UpdateType.MyChatMember && update.MyChatMember?.NewChatMember is ChatMemberMember)
            {
                await botClient.SendTextMessageAsync(update.MyChatMember.Chat.Id, "Hello, I am eventer_bot!");
            }
        }
        public static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}
