using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace bot
{
    public class BotUpdates
    {
        private readonly CommandsHandler _handler;
        public BotUpdates(CommandsHandler handler)
        {
            _handler = handler;
        }
        public async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            
            Console.WriteLine(update.Type);


            if (update.Message is Message message && message.Text != null)
            {
                var commandText = message.Text.Trim('/');
                var commands = _handler.commands;

                if (commands.ContainsKey(commandText)) 
                {
                    await commands[commandText](update.Message.Chat.Id);
                }
            }
           
            else if (update?.Type is UpdateType.MyChatMember && update.MyChatMember?.NewChatMember is ChatMemberMember)
            {
                await botClient.SendTextMessageAsync(update.MyChatMember.Chat.Id, "Hello, I am eventer_bot!");
            }
        }
      
        
        
        
        
        
        
        
        public Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
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
