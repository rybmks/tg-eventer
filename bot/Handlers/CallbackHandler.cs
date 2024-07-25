using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot
{
    public static class CallbackHandler
    {
        public static async Task HandleCallback(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            // Убедитесь, что обновление содержит CallbackQuery
            if (update.CallbackQuery != null)
            {
                var callbackQuery = update.CallbackQuery;
                var callbackData = callbackQuery.Data; // Данные обратного вызова
                var chatId = callbackQuery.Message.Chat.Id; // Идентификатор чата
                var messageId = callbackQuery.Message.MessageId; // Идентификатор сообщения

                Console.WriteLine($"CallbackData: {callbackData}");
                Console.WriteLine($"Message Text: {callbackQuery.Message.Text}");

                // Отправляем ответ на нажатие кнопки
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    text: "Button pressed!",
                    cancellationToken: token
                );

                // Дополнительно: обновляем сообщение, если это необходимо
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"Button pressed! You clicked: {callbackData}",
                    cancellationToken: token
                );
            }
        }
    }
}
