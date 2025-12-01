using Blazor.Components;       // For Blazor Components
using Blazor.Services;         // For BikeService

namespace Blazor
{
    // Entry point of the Blazor application
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a WebApplicationBuilder, which sets up configuration, logging, and services
            var builder = WebApplication.CreateBuilder(args);

            // Add Blazor Server interactive components services
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            // Read the connection string from appsettings.json (DefaultConnection)
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string is missing!");

            // Register BikeService as a singleton in the dependency injection container
            // BikeService will use this connection string to connect to the Neon PostgreSQL database
            builder.Services.AddSingleton(new BikeService(connectionString));

            // Build the application pipeline
            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                // Use custom error page in production
                app.UseExceptionHandler("/Error");

                // Enable HTTP Strict Transport Security (HSTS) for 30 days
                app.UseHsts();
            }

            // Show a custom page for HTTP status codes (like 404 Not Found)
            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            // Redirect HTTP requests to HTTPS
            app.UseHttpsRedirection();

            // Enable anti-forgery protection for forms
            app.UseAntiforgery();

            // Serve static assets (CSS, JS, images)
            app.MapStaticAssets();

            // Map Blazor components for server-side interactive rendering
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            // Run the application
            app.Run();
        }
    }
}
