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


            if (update.Message is Message message && message.Text != null)
            {
                Console.WriteLine(message.Text == "/aboba");
                Console.WriteLine();
                Console.WriteLine($"Received a text message in chat {message.Chat.Id}.");
                Console.WriteLine(update.Type);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "You said:\n" + message.Text,
                    cancellationToken: token);
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
