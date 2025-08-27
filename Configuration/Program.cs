using Microsoft.OpenApi.Models;

namespace CampusNykoping.Configuration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CampusNykoping Configuration API",
                    Version = "v1",
                    Description = "Provides dynamic application configurations and feature flags."
                });
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Configuration API v1");
            });

            // Mock configuration store
            var appConfigs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["ProductCatalog"] = new()
                {
                    ["Currency"] = "SEK",
                    ["ItemsPerPage"] = "20",
                    ["CacheDurationMinutes"] = "10"
                },
                ["Telemetry"] = new()
                {
                    ["RetentionDays"] = "30",
                    ["AlertEmail"] = "alerts@campusnykoping.se"
                }
            };

            // Mock feature flags
            var featureFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["NewCheckoutFlow"] = true,
                ["BetaAnalytics"] = false
            };

            // GET /config/{appName}
            app.MapGet("/config/{appName}", (string appName) =>
            {
                if (appConfigs.TryGetValue(appName, out var config))
                {
                    return Results.Ok(config);
                }
                return Results.NotFound(new { error = $"No config found for app '{appName}'" });
            });

            // GET /features/{featureName}
            app.MapGet("/features/{featureName}", (string featureName) =>
            {
                if (featureFlags.TryGetValue(featureName, out var enabled))
                {
                    return Results.Ok(new Dictionary<string, object>
                    {
                        [featureName] = enabled
                    });
                }
                return Results.NotFound(new { error = $"No feature flag found for '{featureName}'" });
            });

            app.Run();
        }
    }
}