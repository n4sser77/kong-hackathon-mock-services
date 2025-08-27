using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace CampusNykoping.DataProcessing
{
    // Raw telemetry event structure
    public record RawTelemetryEvent(DateTime Timestamp, string Message);

    // Processed event structure
    public record ProcessedEvent(DateTime Timestamp, string Message, string Severity);

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Swagger setup
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CampusNykoping Data Processing API",
                    Version = "v1",
                    Description = "Transforms raw telemetry data into a structured format for analysis."
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Data Processing API v1");
                });
            }

            // In-memory storage for processed events (for demo purposes)
            var processedEvents = new List<ProcessedEvent>();

            // POST /process — accepts a batch of raw events
            app.MapPost("/process", (List<RawTelemetryEvent> rawEvents) =>
            {
                // Simulate data transformation
                foreach (var evt in rawEvents)
                {
                    var severity = evt.Message.Contains("error", StringComparison.OrdinalIgnoreCase)
                        ? "High"
                        : "Normal";

                    processedEvents.Add(new ProcessedEvent(evt.Timestamp, evt.Message, severity));
                }

                Console.WriteLine($"Processed {rawEvents.Count} events.");
                return Results.Accepted(); // 202 Accepted
            });

            // Optional GET to inspect processed results in memory
            app.MapGet("/processed", () => Results.Ok(processedEvents));

            app.Run();
        }
    }
}