using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Npgsql;

namespace Blazor.Models
{
    public class SimpleDbAuthProvider(string connectionString) : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
        private readonly string _connectionString = connectionString;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        public async Task<bool> Login(string email, string password)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT id FROM users WHERE email=@Email AND password_hash=@PasswordHash", conn);
            cmd.Parameters.AddWithValue("Email", email);
            cmd.Parameters.AddWithValue("PasswordHash", Hash(password));

            var idObj = await cmd.ExecuteScalarAsync();
            if (idObj == null || idObj == DBNull.Value) return false;

            var id = idObj.ToString() ?? "0";

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, email),
                new Claim("UserId", id)
            ], "database");

            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

            return true;
        }

        public void Logout()
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        private static string Hash(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
