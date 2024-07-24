using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Net.WebSockets;

namespace bot
{
    internal class Program
    {
        public static readonly TelegramBotClient botClient = new TelegramBotClient("7185443093:AAGJ5X7cHjt2s41uTSPFQwb68i1dL3Pp0hM");
      
        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
           
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] 
                { 
                    UpdateType.Message,
                    UpdateType.MyChatMember,
                    UpdateType.ChatMember
                } 
            };

            CommandsHandler handler = new CommandsHandler(botClient);
            BotUpdates botUpdates = new BotUpdates(handler);

            await handler.SetCommands();


            botClient.StartReceiving(
                botUpdates.Update,
                botUpdates.Error,
                receiverOptions,
                cancellationToken
                );

            Console.WriteLine("bot is started");
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
