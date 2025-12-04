using Blazor.Components;                           // Components namespace
using Blazor.Services;                              // Application services (BikeService, DbConfig)
using Microsoft.AspNetCore.Components.Authorization; // Blazor authentication/authorization types

namespace Blazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the WebApplicationBuilder which configures logging, DI services, and middleware
            var builder = WebApplication.CreateBuilder(args);

            // Add Razor Components and enable interactive Blazor Server functionality
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            // Retrieve database connection string from appsettings.json
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string is missing!");
            
            builder.Services.AddSingleton(new MessageService(connectionString));

            // Register BikeService as a singleton, passing the connection string
            builder.Services.AddSingleton(new BikeService(connectionString));

            // Register DbConfig so it can be injected anywhere the connection string is needed
            builder.Services.AddSingleton(new DbConfig(connectionString));

            // Add basic authorization services required for AuthenticationStateProvider
            builder.Services.AddAuthorizationCore();

            // Register the custom authentication provider as scoped
            // This defines how SimpleDbAuthProvider should be created
            builder.Services.AddScoped<SimpleDbAuthProvider>(sp => new SimpleDbAuthProvider(connectionString));

            // Tell DI that AuthenticationStateProvider = our SimpleDbAuthProvider instance
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SimpleDbAuthProvider>());

            // Build the application with configured services
            var app = builder.Build();

            // Configure error handling for production
            if (!app.Environment.IsDevelopment())
            {
                // Use a custom error page instead of a full stack trace
                app.UseExceptionHandler("/Error");

                // Enable HTTP Strict Transport Security
                app.UseHsts();
            }

            // Provide custom pages for HTTP status codes (404 → /not-found)
            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            // Redirect all HTTP requests to HTTPS
            app.UseHttpsRedirection();

            // Enable antiforgery protection for forms
            app.UseAntiforgery();

            // Serve static files from wwwroot (CSS, JS, images, etc.)
            app.MapStaticAssets();

            // Map the Blazor components and enable interactive server-side rendering
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            // Start the application
            app.Run();
        }
    }
}
