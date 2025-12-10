using Blazor.Models;  // Contains Bike, Message, and User model classes
using Npgsql;          // PostgreSQL data provider

namespace Blazor.Services
{
    // UserService handles all database operations related to users
    public class UserService
    {
        private readonly string _connectionString; // Stores the database connection string

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ------------------------------------------------------------
        // 1. Get all bikes belonging to a specific user
        // ------------------------------------------------------------
        public async Task<List<Bike>> GetUserBikesAsync(int userId)
        {
            var bikes = new List<Bike>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Query bikes by user_id
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

            // Fetch messages for each bike
            foreach (var bike in bikes)
            {
                bike.Messages = await GetMessagesForBikeAsync(bike.Id);
            }

            return bikes;
        }

        // ------------------------------------------------------------
        // 2. Get all messages for a specific bike
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // 3. Sign up a new user
        // Returns true if successful, false if email already exists
        // ------------------------------------------------------------
        public async Task<bool> SignUpAsync(string name, string email, string password)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Check if the email already exists
            await using (var checkCmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE email=@Email", conn))
            {
                checkCmd.Parameters.AddWithValue("Email", email);
                var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

                if (count > 0)
                    return false; // Email already exists
            }

            // Insert new user record
            await using var insertCmd = new NpgsqlCommand(
                @"INSERT INTO users (name, email, password_hash, created_at) 
                  VALUES (@Name, @Email, @PasswordHash, @CreatedAt)", conn);
            insertCmd.Parameters.AddWithValue("Name", name);
            insertCmd.Parameters.AddWithValue("Email", email);
            insertCmd.Parameters.AddWithValue("PasswordHash", Hash(password)); // Store hashed password
            insertCmd.Parameters.AddWithValue("CreatedAt", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();

            return true;
        }

        // ------------------------------------------------------------
        // 4. Hash a password using SHA256
        // ------------------------------------------------------------
        private string Hash(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);          // Convert string to bytes
            var hash = System.Security.Cryptography.SHA256.HashData(bytes); // Compute SHA256 hash
            return Convert.ToHexString(hash);                               // Convert hash to hexadecimal string
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = "SELECT id, name, email, phone, created_at FROM users WHERE id = @UserId";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("UserId", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    ID = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader["name"]?.ToString(),
                    Email = reader["email"]?.ToString(),
                    Phone = reader["phone"]?.ToString(),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }

            return null;
        }
    }
}
