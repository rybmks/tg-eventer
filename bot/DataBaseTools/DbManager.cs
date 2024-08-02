using Npgsql;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot
{
    public class DbManager : IDbManager
    {
        private string _connectionString;
        private ITelegramBotClient botClient = BotServicesContainer.BotClient;
        public DbManager(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<ExecutionStatus> ChangeSubscribe(CallbackQuery callback, string eventName)
        {
            if (callback.From == null || callback.Message == null)
                return ExecutionStatus.NullReferenseError;

            long userId = callback.From.Id;
            long chatId = callback.Message.Chat.Id;

            using var connection = new NpgsqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                var transaction = await connection.BeginTransactionAsync();
                await EnsureUserExistsAsync(callback.From, connection);

                string checkSubExistsQuery = @"
                    SELECT COUNT(*) 
                    FROM subs
                    INNER JOIN event_to_chat etc
                        ON subs.event_to_chat_id = etc.id
                    INNER JOIN events ev
                        ON etc.event_id = ev.event_id
                    WHERE subs.user_id = @userId AND etc.chat_id = @chatId AND ev.event_name = @eventName;";

                long count;
                using (var checkSubExistsCommand = new NpgsqlCommand(checkSubExistsQuery, connection))
                {
                    checkSubExistsCommand.Parameters.AddWithValue("userId", userId);
                    checkSubExistsCommand.Parameters.AddWithValue("chatId", chatId);
                    checkSubExistsCommand.Parameters.AddWithValue("eventName", eventName);

                    var res = await checkSubExistsCommand.ExecuteScalarAsync();
                    count = (res != null) ? (long)res : -1;
                }
                string changeSubQuery;
                if (count > 0)
                {

                    changeSubQuery = @"
                        DELETE FROM subs
                        WHERE user_id = @userId AND event_to_chat_id IN (
                            SELECT etc.id
                            FROM event_to_chat etc
                            INNER JOIN events ev
                                ON etc.event_id = ev.event_id
                            WHERE etc.chat_id = @chatId AND ev.event_name = @eventName);";

                }
                else
                {
                    changeSubQuery = @"
                        INSERT INTO subs (event_to_chat_id, user_id) 
                            SELECT etc.id, @userId
                            FROM event_to_chat etc
                            INNER JOIN events ev
                                ON etc.event_id = ev.event_id
                            WHERE etc.chat_id = @chatId AND ev.event_name = @eventName;";
                }
                using (var changeSubCommand = new NpgsqlCommand(changeSubQuery, connection))
                {
                    changeSubCommand.Parameters.AddWithValue("userId", userId);
                    changeSubCommand.Parameters.AddWithValue("eventName", eventName);
                    changeSubCommand.Parameters.AddWithValue("chatId", chatId);

                    await changeSubCommand.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
                return ExecutionStatus.Success;
            }
            catch (Exception ex)
            {
                BotServicesContainer.Logger.Error($"Method: {nameof(ChangeSubscribe)}. {ex.Message}");
                return ExecutionStatus.DatabaseError;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<ExecutionStatus> CreateEvent(Message message, string eventName)
        {
            User? user = message.From;

            if (user == null || user.Username == null || eventName == null)
                return ExecutionStatus.NullReferenseError;

            long chatId = message.Chat.Id;
            long userId = user.Id;
            using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            string username = user.Username;

            try
            {
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                await EnsureUserExistsAsync(user, connection);

                long event_id = await CheckEventExists(chatId, eventName, connection);

                if (event_id > 0)
                {
                    BotServicesContainer.Logger.Warning("Event already exists");
                    return ExecutionStatus.EventAlreadyExistsError;
                }

                long eventId;

                using (var createEventCommand = new NpgsqlCommand("INSERT INTO events (event_name, created_by) VALUES (@event_name, @created_by) RETURNING event_id;", connection))
                {
                    createEventCommand.Parameters.AddWithValue("event_name", eventName);
                    createEventCommand.Parameters.AddWithValue("created_by", user.Id);

                    var res = await createEventCommand.ExecuteScalarAsync();

                    if (res == null)
                    {
                        BotServicesContainer.Logger.Error("event_id was null");
                        return ExecutionStatus.DatabaseError;
                    }

                    eventId = (long)res;
                }
                using (var linkEventToChatCommand = new NpgsqlCommand("INSERT INTO event_to_chat (chat_id, event_id) VALUES (@chatId, @eventId)", connection))
                {
                    linkEventToChatCommand.Parameters.AddWithValue("chatId", chatId);
                    linkEventToChatCommand.Parameters.AddWithValue("eventId", eventId);

                    await linkEventToChatCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                BotServicesContainer.Logger.Info($"Method {nameof(CreateEvent)}Event added successfully");
                return ExecutionStatus.Success;
            }
            catch (Exception ex)
            {
                BotServicesContainer.Logger.Error(ex.Message);
                return ExecutionStatus.DatabaseError;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<ExecutionStatus> DeleteEvent(Message message, string eventName)
        {
            if (message == null || message.From == null)
                return ExecutionStatus.NullReferenseError;

            long chatId = message.Chat.Id;
            long userId = message.From.Id;
            using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);

            try
            {
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                long eventId = await CheckEventExists(chatId, eventName, connection);

                if (eventId <= 0)
                {
                    BotServicesContainer.Logger.Warning("Event does not exist");
                    return ExecutionStatus.EventDoesNotExistsError;
                }

                var admins = await botClient.GetChatAdministratorsAsync(chatId);
                bool isAdmin = admins.Any(admin => admin.User.Id == userId);

                if (!isAdmin)
                {
                    using (var CheckAuthorOfEventCommand = new NpgsqlCommand("SELECT created_by FROM events WHERE event_id = @eventId;", connection))
                    {
                        CheckAuthorOfEventCommand.Parameters.AddWithValue("eventId", eventId);

                        var result = await CheckAuthorOfEventCommand.ExecuteScalarAsync();

                        long createdBy;
                        long.TryParse(result?.ToString(), out createdBy);

                        if (createdBy != userId)
                        {
                            BotServicesContainer.Logger.Warning("Insufficient rights to operate");
                            return ExecutionStatus.InsufficientRightsError;
                        }
                    }
                }

                string deleteFromSubsQuery = @"
                DELETE FROM subs
                WHERE event_to_chat_id IN (
                    SELECT etc.id
                    FROM event_to_chat etc
                    WHERE etc.event_id = @eventId);";

                using (NpgsqlCommand deleteSubsCommand = new NpgsqlCommand(deleteFromSubsQuery, connection))
                {
                    deleteSubsCommand.Parameters.AddWithValue("eventId", eventId);

                    await deleteSubsCommand.ExecuteNonQueryAsync();
                }

                string deleteFromEventToChatQuery = @"
                DELETE FROM event_to_chat etc
                WHERE event_id = @eventId;";

                using (NpgsqlCommand deleteEventToChatCommand = new NpgsqlCommand(deleteFromEventToChatQuery, connection))
                {
                    deleteEventToChatCommand.Parameters.AddWithValue("eventId", eventId);

                    await deleteEventToChatCommand.ExecuteNonQueryAsync();
                }
                using (NpgsqlCommand deleteEventCommand = new NpgsqlCommand("DELETE FROM events WHERE event_id = @eventId;", connection))
                {
                    deleteEventCommand.Parameters.AddWithValue("eventId", eventId);

                    await deleteEventCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                BotServicesContainer.Logger.Info("Event deleted successfully");
                return ExecutionStatus.Success;
            }
            catch (Exception ex)
            {

                BotServicesContainer.Logger.Error(ex.Message);
                return ExecutionStatus.DatabaseError;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<string[]> GetEventsAsync(long chatId)
        {
            using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            string query = @"
                SELECT e.event_name 
                FROM event_to_chat etc
                INNER JOIN events e ON etc.event_id = e.event_id
                WHERE etc.chat_id = @chatId;";

            using NpgsqlCommand command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("chatId", chatId);

            using NpgsqlDataReader dataReader = await command.ExecuteReaderAsync();

            if (!dataReader.HasRows)
                return Array.Empty<string>();

            using DataTable dataTable = new DataTable();
            dataTable.Load(dataReader);

            var events = dataTable.AsEnumerable()
                .Select(row => row.Field<string>("event_name") ?? string.Empty);

            return events.ToArray();
        }
        public async Task<ExecutionStatus> UnsubscribeAll(CallbackQuery callback)
        {
            if (callback.From == null || callback.Message == null)
                return ExecutionStatus.NullReferenseError;

            long chatId = callback.Message.Chat.Id;
            long userId = callback.From.Id;

            string query = @"
                DELETE FROM subs
                WHERE user_id = @userId AND event_to_chat_id IN (
                    SELECT id
                    FROM event_to_chat
                    WHERE chat_id = @chatId);";

            using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                var transaction = await connection.BeginTransactionAsync();

                using (var deleteSubsCommand = new NpgsqlCommand(query, connection))
                {
                    deleteSubsCommand.Parameters.AddWithValue("userId", userId);
                    deleteSubsCommand.Parameters.AddWithValue("chatId", chatId);

                    var res = await deleteSubsCommand.ExecuteScalarAsync();
                    //5902475042
                    BotServicesContainer.Logger.Info($"\t {res}"); // -1001243651779
                }

                await transaction.CommitAsync();
                return ExecutionStatus.Success;
            }
            catch (Exception ex)
            {
                BotServicesContainer.Logger.Error($"Method {nameof(UnsubscribeAll)}. {ex.Message}");
                return ExecutionStatus.DatabaseError;
            }
            finally
            {
                connection.Close();
            }
        }
        private async Task EnsureUserExistsAsync(User user, NpgsqlConnection connection)
        {
            string? username = user?.Username;

            if (user == null || username == null)
            {
                BotServicesContainer.Logger.Warning("Null ref");
                return;
            }

            long userId = user.Id;
            try
            {
                using (var getUserCommand = new NpgsqlCommand("SELECT * FROM users WHERE id = @userid;", connection))
                {
                    getUserCommand.Parameters.AddWithValue("userid", user.Id);

                    using NpgsqlDataReader dataReader = await getUserCommand.ExecuteReaderAsync();

                    if (!dataReader.HasRows)
                    {
                        using (var insertUserCommand = new NpgsqlCommand("INSERT INTO users (id, username) VALUES (@id, @username);", connection))
                        {
                            insertUserCommand.Parameters.AddWithValue("id", userId);
                            insertUserCommand.Parameters.AddWithValue("username", username);

                            await insertUserCommand.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        using DataTable dataTable = new DataTable();
                        dataTable.Load(dataReader);

                        string? usernameFromDb = dataTable.Rows[0][0].ToString();

                        if (usernameFromDb != null && username != usernameFromDb)
                        {
                            using (NpgsqlCommand updateUsername = new NpgsqlCommand("UPDATE users SET username = @newusername WHERE id = @id", connection))
                            {
                                updateUsername.Parameters.AddWithValue("newusername", username);
                                updateUsername.Parameters.AddWithValue("id", user.Id);

                                await updateUsername.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BotServicesContainer.Logger.Error(ex.Message);
                return;
            }
        }
        private async Task<long> CheckEventExists(long chatId, string eventName, NpgsqlConnection connection)
        {
            string query = @"
                SELECT e.event_id
                FROM event_to_chat etc
                INNER JOIN events e ON etc.event_id = e.event_id 
                WHERE etc.chat_id = @chatId AND e.event_name = @eventName;";

            using (var isEventExistsCommand = new NpgsqlCommand(query, connection))
            {
                isEventExistsCommand.Parameters.AddWithValue("eventName", eventName);
                isEventExistsCommand.Parameters.AddWithValue("chatId", chatId);

                var res = await isEventExistsCommand.ExecuteScalarAsync();
                var id = res as long? ?? 0;

                return id > 0 ? id : -1;
            }
        }
        public async Task<List<string>> ShowMySubs(CallbackQuery callback)
        {
            List<string> subs = new List<string>();


            if (callback.From == null || callback.Message == null)
                return subs;

            long userId = callback.From.Id;
            long chatId = callback.Message.Chat.Id;
            using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                var transaction = await connection.BeginTransactionAsync();

                string selectQuery = @"
                    SELECT event_name 
                    FROM subs 
                    INNER JOIN event_to_chat etc 
                        ON subs.event_to_chat_id = etc.id 
                    INNER JOIN events ev
                        ON etc.event_id = ev.event_id
                    WHERE subs.user_id = @userId AND etc.chat_id = @chatId;";

                using (var getSubsCommand = new NpgsqlCommand(selectQuery, connection))
                {
                    getSubsCommand.Parameters.AddWithValue("userId", userId);
                    getSubsCommand.Parameters.AddWithValue("chatId", chatId);

                    using (NpgsqlDataReader reader = await getSubsCommand.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            subs.Add(reader.GetString(0));
                        }
                    }
                }
                await transaction.CommitAsync();
                return subs;
            }
            catch (Exception ex)
            {
                BotServicesContainer.Logger.Error($"Method {nameof(ShowMySubs)}. {ex.Message}");
                return new List<string>();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<List<string>> Ping(Message message, string eventName)
        {
            List<string> usernames = new List<string>();

            if (message == null || eventName == null)
                return usernames;

            long chatId = message.Chat.Id;

            using var connection = new NpgsqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                var transaction = await connection.BeginTransactionAsync();

                string query = @"
                    SELECT username 
                    FROM subs
                    INNER JOIN users 
                        ON subs.user_id = users.id
                    WHERE event_to_chat_id IN(
                    SELECT id FROM event_to_chat etc
                    INNER JOIN events 
                        ON etc.event_id = events.event_id
                    WHERE events.event_name = @eventName AND etc.chat_id = @chatId);";

                using (var getUsers = new NpgsqlCommand(query, connection))
                {
                    getUsers.Parameters.AddWithValue("eventName", eventName);
                    getUsers.Parameters.AddWithValue("chatId", chatId);

                    using (NpgsqlDataReader reader = await getUsers.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            usernames.Add(reader.GetString(0));
                        }
                    }
                }
                transaction.Commit();
                return usernames;
            }
            catch (Exception ex)
            {
                BotServicesContainer.Logger.Error($"Method {nameof(ShowMySubs)}. {ex.Message}");
                return new List<string>();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
