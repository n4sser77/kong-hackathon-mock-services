using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

// Simple event model
public record TelemetryEvent(DateTime Timestamp, string Message);

namespace Telemetry
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Swagger if you want quick docs
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            // In-memory event store for this mockup
            var events = new List<TelemetryEvent>();

            // POST /events — accepts telemetry payload
            app.MapPost("/events", (TelemetryEvent telemetryEvent) =>
            {
                events.Add(telemetryEvent);
                Console.WriteLine($"[{telemetryEvent.Timestamp:O}] {telemetryEvent.Message}");
                return Results.Ok();
            });

            // Optional: GET /events — to inspect ingested events during testing
            app.MapGet("/events", () => Results.Ok(events));

            app.Run();
        }
    }
}