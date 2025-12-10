using Blazor.Models; // Import the Bike, User, and Message models
using Npgsql; // Import PostgreSQL connectivity

namespace Blazor.Services
{
    // Service responsible for sending and retrieving messages from the database
    public class MessageService(string connectionString)
    {
        private readonly string _connectionString = connectionString; // PostgreSQL connection string

        // ------------------------------------------------------------
        // Retrieves all messages related to a specific bike asynchronously
        // ------------------------------------------------------------
        public async Task<List<Message>> GetMessagesAsync(int bikeId)
        {
            var messages = new List<Message>(); // Initialize the list of messages

            // Create and open a new PostgreSQL connection asynchronously
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Prepare the SQL query to fetch messages and their senders/receivers
            await using var cmd = new NpgsqlCommand(@"
                SELECT m.id, m.from_user, m.to_user, m.content, m.created_at,
                       u1.name AS from_name, u2.name AS to_name
                FROM messages m
                LEFT JOIN users u1 ON u1.id = m.from_user
                LEFT JOIN users u2 ON u2.id = m.to_user
                WHERE m.bike_id = @bikeId
                ORDER BY m.created_at ASC
            ", conn);

            // Bind the bikeId parameter to prevent SQL injection
            cmd.Parameters.AddWithValue("bikeId", bikeId);

            // Execute the query and read results asynchronously
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Map each row from the database to a Message object
                messages.Add(new Message
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")), // Message ID
                    BikeId = bikeId, // Bike ID related to the message
                    FromUserId = reader.GetInt32(reader.GetOrdinal("from_user")), // Sender ID
                    ToUserId = reader.GetInt32(reader.GetOrdinal("to_user")), // Receiver ID
                    Content = reader["content"]?.ToString(), // Message text
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")), // Timestamp
                    FromUser = new User { Name = reader["from_name"]?.ToString() }, // Sender details
                    ToUser = new User { Name = reader["to_name"]?.ToString() } // Receiver details
                });
            }

            return messages; // Return the list of messages
        }

        // ------------------------------------------------------------
        // Inserts a new message into the database asynchronously
        // ------------------------------------------------------------
        public async Task SendMessageAsync(Message message)
        {
            // Open a PostgreSQL connection
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Prepare the SQL insert command
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO messages (from_user, to_user, bike_id, content, created_at)
                VALUES (@from, @to, @bike, @content, @createdAt)
            ", conn);

            // Bind all parameters safely to prevent SQL injection
            cmd.Parameters.AddWithValue("from", message.FromUserId); // Sender ID
            cmd.Parameters.AddWithValue("to", message.ToUserId); // Receiver ID
            cmd.Parameters.AddWithValue("bike", message.BikeId); // Bike ID
            cmd.Parameters.AddWithValue("content", message.Content ?? ""); // Message text (nullable check)
            cmd.Parameters.AddWithValue("createdAt", message.CreatedAt); // Timestamp

            // Execute the insert command asynchronously
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
