using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text;
using System.Buffers;
using Newtonsoft.Json.Linq;

namespace bot
{
    public static class CallbackHandler
    {
        private static IDbManager db;
        private static Interfaces.ILogger logger;
        static CallbackHandler()
        {
            if (BotServicesContainer.BotClient == null)
            {
                throw new ArgumentNullException(nameof(BotServicesContainer.BotClient), "BotClient cannot be null");
            }

            logger = BotServicesContainer.Logger;
            db = new DbManager(BotServicesContainer.ConnectionString);
        }
        public static async Task HandleCallback(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            if (update.CallbackQuery == null || update.CallbackQuery.Data == null || update.CallbackQuery.Message == null)
                return;
            

            var callbackQuery = update.CallbackQuery;
            string callbackData = callbackQuery.Data;
            long chatId = update.CallbackQuery.Message.Chat.Id; 

            if (callbackData == "command_unsub")
            {
                ExecutionStatus status = await db.UnsubscribeAll(callbackQuery);
                string mes;

                if (status == ExecutionStatus.Success)
                    mes = "Good!";
               
                else
                    mes = "Some Error!";

                await botClient.AnswerCallbackQueryAsync(
                             callbackQuery.Id,
                             text: mes,
                             cancellationToken: token
                );
            }
            else if (callbackData == "command_mysubs")
            {
                DisplayMySubs(callbackQuery, botClient, await db.ShowMySubs(callbackQuery));
            }
            else
            {
                ExecutionStatus status = await db.ChangeSubscribe(callbackQuery, callbackData);

                string mes;
                if (status == ExecutionStatus.Success)
                    mes = "Subscribe status was changed!";
             
                else 
                    mes = "Some Error!";
                
                await botClient.AnswerCallbackQueryAsync(
                             callbackQuery.Id,
                             text: mes,
                             cancellationToken: token
                );
            }
        }
        private static async void DisplayMySubs(CallbackQuery callback, ITelegramBotClient botClient, List<string> subs)
        {
            if (callback.Message == null)
                return;
            
            if (subs.Count < 1) 
            {
                await botClient.AnswerCallbackQueryAsync(
                             callback.Id,
                             text: "You dont have any subs now!"
                );
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Your subs:");

            foreach (var sub in subs)
            {
                sb.Append(" " + sub + ",");
            }

            await botClient.AnswerCallbackQueryAsync(
                             callback.Id,
                             text: sb.ToString().Trim(',')
                );
        }
    }
}
