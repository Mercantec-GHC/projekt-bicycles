using Blazor.Models;                  // Include the Models namespace (for SimpleDbAuthProvider, etc.)
using Blazor.Components;              // Include Blazor Components namespace
using Blazor.Services;                // Include Services namespace (BikeService, DbConfig)
using Microsoft.AspNetCore.Components.Authorization; // Include Blazor authorization types (AuthenticationStateProvider)

namespace Blazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a new WebApplicationBuilder which sets up the app configuration, logging, and services
            var builder = WebApplication.CreateBuilder(args);

            // Add services required for interactive Blazor Server components
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            // Get the connection string from appsettings.json (DefaultConnection)
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string is missing!"); // Throw error if missing

            // Register BikeService as a singleton in DI container using the connection string
            builder.Services.AddSingleton(new BikeService(connectionString));

            // Register DbConfig as a singleton so other components/services can access the connection string
            builder.Services.AddSingleton(new DbConfig(connectionString));

            // Add core authorization services (required for AuthenticationStateProvider)
            builder.Services.AddAuthorizationCore();

            // Register the SimpleDbAuthProvider as the AuthenticationStateProvider in the DI container
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => new SimpleDbAuthProvider(connectionString));

            // Build the WebApplication pipeline
            var app = builder.Build();

            // Configure error handling and HSTS for production environments
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error"); // Show custom error page
                app.UseHsts();                     // Enable HTTP Strict Transport Security
            }

            // Show a custom page for HTTP status codes like 404
            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            // Redirect HTTP requests to HTTPS
            app.UseHttpsRedirection();

            // Enable antiforgery protection for forms
            app.UseAntiforgery();

            // Serve static files (CSS, JS, images)
            app.MapStaticAssets();

            // Map Blazor components for server-side rendering
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            // Start the application
            app.Run();
        }
    }
}
