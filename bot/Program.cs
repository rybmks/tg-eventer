using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.WebSockets;

namespace bot
{
    internal class Program
    {
        
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddJsonFile(@"C:/Users/rybal/source/repos/eventer_bot/cfg/appsettings.json").Build();
            
            string? token = config["TelegramBot:Token"];

            if (token == null) 
            {
                Console.WriteLine("Set token to cfg!");
                return;
            }

            TelegramBotClient botClient = new TelegramBotClient(token);

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
