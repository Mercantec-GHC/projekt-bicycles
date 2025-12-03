using Microsoft.AspNetCore.Components.Authorization; // Provides the base class for authentication state
using System.Security.Claims; // Provides ClaimsPrincipal and ClaimsIdentity
using Npgsql;

namespace Blazor.Services
{
    // Authentication provider that manages user login/logout using a PostgreSQL database
    public class SimpleDbAuthProvider(string connectionString) : AuthenticationStateProvider
    {
        // Stores the currently logged-in user
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        // Connection string for PostgreSQL database
        private readonly string _connectionString = connectionString;

        // Returns the current authentication state (logged-in user)
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // Logs in a user with email and password
        public async Task<bool> Login(string email, string password)
        {
            // Create and open a PostgreSQL connection
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Prepare SQL command to select user ID by email and password hash
            using var command = new NpgsqlCommand("SELECT id FROM users WHERE email=@Email AND password_hash=@PasswordHash", connection);
            command.Parameters.AddWithValue("Email", email);
            command.Parameters.AddWithValue("PasswordHash", Hash(password));

            // Execute command and get user ID
            var idObj = await command.ExecuteScalarAsync();
            if (idObj == null || idObj == DBNull.Value) return false; // User not found

            var id = idObj.ToString() ?? "0";

            // Create claims identity for the logged-in user
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, email ?? ""), new Claim("UserId", id)], "database"); // Authentication type

            // Update the current user
            _currentUser = new ClaimsPrincipal(identity);

            // Notify Blazor that authentication state has changed
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

            return true; // Login successful
        }

        // Logs out the current user
        public void Logout()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity()); // Reset to empty identity
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // Hashes the password using SHA256
        private string Hash(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input); // Convert string to bytes
            var hash = System.Security.Cryptography.SHA256.HashData(bytes); // Compute SHA256 hash
            return Convert.ToHexString(hash); // Convert hash to hex string
        }
    }
}
