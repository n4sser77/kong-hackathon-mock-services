// File: Program.cs

using Microsoft.AspNetCore.Mvc;
using Bogus;
using NSwag.Generation.Processors.Security;
using NSwag.Generation.Processors;
using NSwag.AspNetCore;

// --- Mock Data Generation with Bogus ---
// Set a fixed seed for consistent, repeatable data generation.
Randomizer.Seed = new Random(101);

// Create a Faker for generating realistic User objects.
var userFaker = new Faker<User>()
    .RuleFor(u => u.Id, f => f.Random.Guid())
    .RuleFor(u => u.FirstName, f => f.Person.FirstName)
    .RuleFor(u => u.LastName, f => f.Person.LastName)
    .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
    .RuleFor(u => u.Role, f => f.PickRandom("Customer", "Employee", "Partner", "Admin"))
    .RuleFor(u => u.CreatedAt, f => f.Date.Recent(30));

// Generate a static, in-memory list of mock users.
var mockUsers = userFaker.Generate(500000);

// --- API Setup ---
// Standard builder for a minimal API application.
var builder = WebApplication.CreateBuilder(args);

// Add the services for NSwag and OpenAPI documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
   config.DocumentName = "ReportingAPI";
   config.Title = "Reporting Service API";
   config.Version = "v1";
});

var app = builder.Build();

// --- Middleware Configuration ---
// Configure the HTTP request pipeline for development.
if (app.Environment.IsDevelopment())
{
   // Use the NSwag middleware.
   app.UseOpenApi();
   app.UseSwaggerUi(config =>
   {
      config.DocumentTitle = "Reporting Service API";
      config.Path = "/swagger";
      config.DocumentPath = "/swagger/{documentName}/swagger.json";
      config.DocExpansion = "list";
   });
}

app.UseHttpsRedirection();

// --- Reporting & Analytics API Endpoints ---
// GET /reports/daily-active-users
app.MapGet("/reports/daily-active-users", () =>
{
   var activeUsers = mockUsers.Where(u => (DateTime.UtcNow - u.CreatedAt).TotalDays < 1);
   var report = new DailyActiveUsersReport(
       Date: DateTime.UtcNow.Date,
       Count: activeUsers.Count(),
       UserIds: activeUsers.Select(u => u.Id).ToList()
   );
   return Results.Ok(report);
})
.WithName("GetDailyActiveUsersReport");


// GET /reports/errors-by-service
app.MapGet("/reports/errors-by-service", ([FromQuery] string serviceName) =>
{
   // Mock data for demonstration
   var errors = new List<object>
    {
        new { Service = "User Identity", Count = 15, LastError = DateTime.UtcNow.AddMinutes(-5) },
        new { Service = "Billing", Count = 5, LastError = DateTime.UtcNow.AddHours(-2) },
        new { Service = "Reporting", Count = 0, LastError = (DateTime?)null }
    };
   var result = errors.FirstOrDefault(e => e.GetType().GetProperty("Service")?.GetValue(e)?.ToString() == serviceName);

   return result is null ? Results.NotFound() : Results.Ok(result);
})
.WithName("GetErrorsByServiceReport");


app.Run();

// --- Data Models ---
// The User type is now a record class with a primary constructor and an empty constructor.
// Bogus can now instantiate this class using the parameterless constructor.
public record class User(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTime CreatedAt)
{
   public User() : this(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, DateTime.MinValue) { }
}

// The NewUserDto is now a record class with a primary constructor.
public record class NewUserDto(
    Guid? Id,
    string FirstName,
    string LastName,
    string Email
);

public record UserUpdateRoleDto(
    string Role
);

// New data model for the reporting endpoint.
public record DailyActiveUsersReport(
    DateTime Date,
    int Count,
    List<Guid> UserIds
);
