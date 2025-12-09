using Blazor.Models;
using Npgsql;

namespace Blazor.Services
{
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Bike>> GetUserBikesAsync(int userId)
        {
            var bikes = new List<Bike>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"SELECT * FROM bikes WHERE user_id = @userId ORDER BY created_at DESC";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                bikes.Add(new Bike
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    Title = reader["title"]?.ToString(),
                    Price = reader.GetDecimal(reader.GetOrdinal("price")),
                    Color = reader["color"]?.ToString(),
                    Type = reader["type"]?.ToString(),
                    BikeCondition = reader["bike_condition"]?.ToString(),
                    Brand = reader["brand"]?.ToString(),
                    Location = reader["location"]?.ToString(),
                    ImageUrl = reader["image_url"]?.ToString(),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            foreach (var bike in bikes)
            {
                bike.Messages = await GetMessagesForBikeAsync(bike.Id);
            }

            return bikes;
        }

        public async Task<List<Message>> GetMessagesForBikeAsync(int bikeId)
        {
            var messages = new List<Message>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT m.id, m.content, m.from_user, u.email AS from_email, m.to_user, m.bike_id, m.created_at
                FROM messages m
                JOIN users u ON m.from_user = u.id
                WHERE m.bike_id = @bikeId
                ORDER BY m.created_at DESC;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("bikeId", bikeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(new Message
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Content = reader["content"]?.ToString(),
                    FromUserId = reader.GetInt32(reader.GetOrdinal("from_user")),
                    ToUserId = reader.GetInt32(reader.GetOrdinal("to_user")),
                    BikeId = reader.GetInt32(reader.GetOrdinal("bike_id")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    FromUser = new User { Email = reader["from_email"]?.ToString() }
                });
            }

            return messages;
        }
    }
}