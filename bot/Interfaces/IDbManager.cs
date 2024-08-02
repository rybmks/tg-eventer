using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace bot
{
    public interface IDbManager
    {
        public Task<ExecutionStatus> CreateEvent(Message message, string eventName);
        public Task<ExecutionStatus> DeleteEvent(Message message, string eventName);
        public Task<ExecutionStatus> ChangeSubscribe(CallbackQuery callback, string eventName);
        public Task<ExecutionStatus> UnsubscribeAll(CallbackQuery callback);
        public Task<List<string>> ShowMySubs(CallbackQuery callback);
        public Task<List<string>> Ping(Message message, string eventName);
        public Task<string[]> GetEventsAsync(long chatId);
    }
}
