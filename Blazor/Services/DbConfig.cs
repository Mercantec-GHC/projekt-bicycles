namespace Blazor.Services
{
    public class DbConfig
    {
        public string ConnectionString { get; }
        public DbConfig(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
