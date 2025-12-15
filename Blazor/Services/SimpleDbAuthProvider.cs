using Microsoft.AspNetCore.Components.Authorization; // Provides AuthenticationStateProvider and AuthenticationState
using System.Security.Claims; // Provides ClaimsPrincipal and ClaimsIdentity for user identity
using Npgsql; // Provides PostgreSQL database connectivity

namespace Blazor.Services
{
    // Authentication provider that handles login/logout using a PostgreSQL database
    public class SimpleDbAuthProvider(string connectionString) : AuthenticationStateProvider
    {
        private int _currentUserId; // Stores the ID of the currently logged-in user

        // _currentUser holds the current user's ClaimsPrincipal (empty by default)
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());// like we make new user and user's identity (pas for example) where data saves (Claims)

        private readonly string _connectionString = connectionString; // Stores the PostgreSQL connection string

        // Property to access the current user ID from outside the class
        public int CurrentUserId => _currentUserId;

        // Returns the current authentication state (used by Blazor's AuthorizeView, etc.)
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Wrap _currentUser in AuthenticationState and return as a completed task, not async 
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // ------------------------------------------------------------
        // Login a user using email and password
        // Returns true if login succeeds, false if credentials are invalid
        // ------------------------------------------------------------
        public async Task<bool> Login(string email, string password)
        {
            // Create and open PostgreSQL connection
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Prepare SQL command to select user ID where email and password hash match
            using var command = new NpgsqlCommand(
                "SELECT id FROM users WHERE email=@Email AND password_hash=@PasswordHash", connection);

            command.Parameters.AddWithValue("Email", email);           // Bind email parameter
            command.Parameters.AddWithValue("PasswordHash", Hash(password)); // Bind hashed password

            // Execute the query and get the user ID (or null if not found)
            object? idObj = await command.ExecuteScalarAsync(); // returns first column of first row in result set, object, null or DBNull.Value 
            if (idObj == null || idObj == DBNull.Value) return false; // Login failed

            _currentUserId = Convert.ToInt32(idObj); // Save current user ID from object to int

            // Create claims identity for the logged-in user
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, email ?? ""),           // Store user's email as claim
                    new Claim("UserId", _currentUserId.ToString())    // Store user ID as claim
                },
                "database"); // Authentication type is "database"

            _currentUser = new ClaimsPrincipal(identity); // Wrap identity into a ClaimsPrincipal

            // Notify Blazor that the authentication state has changed
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

            return true; // Login successful
        }

        // ------------------------------------------------------------
        // Logout the current user
        // Clears current user ID and ClaimsPrincipal
        // ------------------------------------------------------------
        public void Logout()
        {
            _currentUserId = 0; // Reset user ID
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity()); // Reset ClaimsPrincipal (empty)

            // Notify Blazor of logout so UI updates (hides AuthorizeView content, etc.)
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // ------------------------------------------------------------
        // Hashes a string using SHA256 (used for password hashing)
        // ------------------------------------------------------------
        private static string Hash(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);        // Convert input string to bytes
            var hash = System.Security.Cryptography.SHA256.HashData(bytes); // Compute SHA256 hash
            return Convert.ToHexString(hash);                              // Convert hash bytes to hexadecimal string
        }
    }
}
