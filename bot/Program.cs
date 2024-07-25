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
using System.Reflection;

namespace bot
{
    internal class Program
    {
        
        static async Task Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string projectRootDirectory = GetProjectRootDirectory(currentDirectory);

            string cfgPath = Path.Combine(projectRootDirectory, "cfg", "appsettings.json");


            var config = new ConfigurationBuilder().AddJsonFile(cfgPath).Build();
            
            string? token = config["TelegramBot:Token"];

            if (token == null) 
            {
                Console.WriteLine("Set token to cfg!");
                return;
            }

            TelegramBotClient botClient = new TelegramBotClient(token);
            BotServicesContainer.BotClient = botClient;

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
           
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] 
                { 
                    UpdateType.Message,
                    UpdateType.MyChatMember,
                    UpdateType.ChatMember,
                    UpdateType.CallbackQuery
                } 
            };

            await CommandsHandler.ShowCommands();

            botClient.StartReceiving(
                BotUpdates.Update,
                BotUpdates.Error,
                receiverOptions,
                cancellationToken
                );

            Console.WriteLine("bot is started");
            Console.ReadLine();
            cts.Cancel();
        }
        private static string GetProjectRootDirectory(string startDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(startDirectory);
            while (directoryInfo.Parent != null)
            {
                if (Directory.Exists(Path.Combine(directoryInfo.FullName, "cfg")))
                {
                    return directoryInfo.FullName;
                }
                directoryInfo = directoryInfo.Parent;
            }
            throw new DirectoryNotFoundException("Project root directory with 'cfg' not found.");
        }
    }
}
