using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Npgsql;

namespace Blazor.Services
{
    // Authentication provider that manages user login/logout using a PostgreSQL database
    public class SimpleDbAuthProvider : AuthenticationStateProvider
    {
        private int _currentUserId;
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity()); // текущий пользователь
        private readonly string _connectionString;

        public SimpleDbAuthProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Возвращает текущий ID пользователя
        public int CurrentUserId => _currentUserId;

        public void SetCurrentUserId(int id) => _currentUserId = id;

        // Возвращает состояние аутентификации
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // Вход пользователя по email и паролю
        public async Task<bool> Login(string email, string password)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand(
                "SELECT id FROM users WHERE email=@Email AND password_hash=@PasswordHash", connection);

            command.Parameters.AddWithValue("Email", email);
            command.Parameters.AddWithValue("PasswordHash", Hash(password));

            var idObj = await command.ExecuteScalarAsync();
            if (idObj == null || idObj == DBNull.Value) return false;

            _currentUserId = Convert.ToInt32(idObj);

            // Создаём ClaimsIdentity для пользователя
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, email ?? ""),
                    new Claim("UserId", _currentUserId.ToString())
                }, "database");

            _currentUser = new ClaimsPrincipal(identity);

            // Уведомляем Blazor о смене состояния аутентификации
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

            return true;
        }

        // Выход пользователя
        public void Logout()
        {
            _currentUserId = 0;
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // Хэширование пароля SHA256
        private static string Hash(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
