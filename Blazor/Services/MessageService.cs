using Blazor.Models;
using Npgsql;

namespace Blazor.Services
{
    public class MessageService
    {
        private readonly string _connectionString;

        public MessageService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Асинхронно получить все сообщения для конкретного байка
        public async Task<List<Message>> GetMessagesAsync(int bikeId)
        {
            var messages = new List<Message>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT m.id, m.from_user, m.to_user, m.content, m.created_at,
                       u1.name AS from_name, u2.name AS to_name
                FROM messages m
                LEFT JOIN users u1 ON u1.id = m.from_user
                LEFT JOIN users u2 ON u2.id = m.to_user
                WHERE m.bike_id = @bikeId
                ORDER BY m.created_at ASC
            ", conn);

            cmd.Parameters.AddWithValue("bikeId", bikeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(new Message
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    BikeId = bikeId,
                    FromUserId = reader.GetInt32(reader.GetOrdinal("from_user")),
                    ToUserId = reader.GetInt32(reader.GetOrdinal("to_user")),
                    Content = reader["content"]?.ToString(),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    FromUser = new User { Name = reader["from_name"]?.ToString() },
                    ToUser = new User { Name = reader["to_name"]?.ToString() }
                });
            }

            return messages;
        }

        // Асинхронно отправить новое сообщение
        public async Task SendMessageAsync(Message message)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO messages (from_user, to_user, bike_id, content, created_at)
                VALUES (@from, @to, @bike, @content, @createdAt)
            ", conn);

            cmd.Parameters.AddWithValue("from", message.FromUserId);
            cmd.Parameters.AddWithValue("to", message.ToUserId);
            cmd.Parameters.AddWithValue("bike", message.BikeId);
            cmd.Parameters.AddWithValue("content", message.Content ?? "");
            cmd.Parameters.AddWithValue("createdAt", message.CreatedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        // Получить ID владельца байка
        public async Task<int> GetBikeOwnerIdAsync(int bikeId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT user_id FROM bikes WHERE id=@bikeId", conn);
            cmd.Parameters.AddWithValue("bikeId", bikeId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}
