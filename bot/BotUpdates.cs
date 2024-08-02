using System.Security.Cryptography;
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
            //игнор сообщений, отправленных до старта бота 
            if (BotServicesContainer.StartedAt != DateTime.MinValue) 
            {
                if (update.Message != null)
                {
                    TimeSpan diff = BotServicesContainer.StartedAt.ToUniversalTime() - update.Message.Date.ToUniversalTime();
                 
                    if (diff.TotalSeconds > 10)
                        return;
                }
                else if (update.CallbackQuery != null && update.CallbackQuery.Message != null)
                {
                    var now = DateTime.UtcNow;
                    var started = BotServicesContainer.StartedAt.ToUniversalTime();

                    //Невозможно отследить время нажатия на кнопку, время которое вернет Message.Date == времени отправки сообщения с кнопкой.
                    //Поэтому, чтобы игнорировать нажатия, которые были сделаны до старта бота, отсеиваем то, что приходит на момент запуска бота 
                    if (Math.Abs((now - started).TotalSeconds) < 3) 
                        return;
                }
            }
            BotServicesContainer.Logger.Info($"Received update of type {update.Type}. Chat id: {update?.Message?.Chat.Id}");

            if (update?.Message?.Chat.Type is ChatType.Private) 
            { 
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Add me to a chat to start work!");
                return;
            }
            
            if (update?.CallbackQuery != null)
            {
                await CallbackHandler.HandleCallback(botClient, update, token);
                return;
            }
          
            if (update?.Message is Message message && message.Text != null && message.Text.StartsWith('/'))
            {
                var commandText = message.Text.Split('@', ' ')[0].Trim('/').ToLower();
                var commands = CommandsHandler.commands;

                if (commands != null && commands.ContainsKey(commandText))
                {
                    await commands[commandText](message);
                    return;
                }
            }
            if (update?.Type is UpdateType.MyChatMember && update.MyChatMember?.NewChatMember is ChatMemberMember)
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

            BotServicesContainer.Logger.Error(errorMessage);
            return Task.CompletedTask;
        }
    }
}
