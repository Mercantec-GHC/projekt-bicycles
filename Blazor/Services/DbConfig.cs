namespace Blazor.Services
{
    public class DbConfig
    {
        // Public property that stores the database connection string.
        // Other services can access this value through dependency injection.
        public string ConnectionString { get; }

        // Constructor that receives the connection string and assigns it
        // to the ConnectionString property.
        // This object is usually registered as a singleton in Program.cs,
        // so all services can access the same shared configuration.
        public DbConfig(string connectionString)
        {
            ConnectionString = connectionString; // Save the provided connection string.
        }
    }
}
