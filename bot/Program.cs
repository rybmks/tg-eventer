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
using bot.Interfaces;

namespace bot
{
    internal class Program
    {
        
        static async Task Main(string[] args)
        {

            ILogger logger = new ConsoleLogger();


            string currentDirectory = Directory.GetCurrentDirectory();
            string projectRootDirectory = GetProjectRootDirectory(currentDirectory);
            string cfgPath = Path.Combine(projectRootDirectory, "cfg", "appsettings.json");


            var config = new ConfigurationBuilder().AddJsonFile(cfgPath).Build();
            
            string? token = config["TelegramBot:Token"];
            string? connectionString = config["TelegramBot:ConnectionString"];
            if (token == null || connectionString == null) 
            {
                logger.Error("Set token and connection string to cfg!");
                return;
            }
            BotServicesContainer.ConnectionString = connectionString;
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

            BotServicesContainer.StartedAt = DateTime.Now.ToUniversalTime();
            botClient.StartReceiving(
                BotUpdates.Update,
                BotUpdates.Error,
                receiverOptions,
                cancellationToken
                );

            logger.Info("bot is started");
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
