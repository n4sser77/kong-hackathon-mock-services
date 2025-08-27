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

// Generate a static, in-memory list of 50 mock users.
var mockUsers = userFaker.Generate(50);

// --- API Setup ---
// Standard builder for a minimal API application.
var builder = WebApplication.CreateBuilder(args);

// Add the services for NSwag and OpenAPI documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "UserAPI";
    config.Title = "User Service API";
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
        config.DocumentTitle = "User Service API";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

app.UseHttpsRedirection();

// --- API Endpoints ---
// GET /users - List all users
app.MapGet("/users", () =>
{
    return Results.Ok(mockUsers);
})
.WithName("GetAllUsers");

// GET /users/{userId}
// Retrieves a single user by their unique ID.
app.MapGet("/users/{userId}", ([FromRoute] Guid userId) =>
{
    var user = mockUsers.FirstOrDefault(u => u.Id == userId);
    return user is null ? Results.NotFound() : Results.Ok(user);
})
.WithName("GetUserById");

// POST /users
// Creates a new user.
app.MapPost("/users", ([FromBody] NewUserDto newUser) =>
{
    // Note: The primary constructor of a record class is used here.
    var createdUser = new User(
        Id: Guid.NewGuid(),
        FirstName: newUser.FirstName,
        LastName: newUser.LastName,
        Email: newUser.Email,
        Role: "Customer",
        CreatedAt: DateTime.UtcNow
    );
    mockUsers.Add(createdUser);
    return Results.Created($"/users/{createdUser.Id}", createdUser);
})
.WithName("CreateUser");

// PUT /users/{userId}/role
// Updates a user's role.
app.MapPut("/users/{userId}/role", ([FromRoute] Guid userId, [FromBody] UserUpdateRoleDto roleDto) =>
{
    var userIndex = mockUsers.FindIndex(u => u.Id == userId);
    if (userIndex == -1)
    {
        return Results.NotFound();
    }
    var oldUser = mockUsers[userIndex];
    var updatedUser = oldUser with { Role = roleDto.Role }; // Using `with` for non-destructive mutation on a record.
    mockUsers[userIndex] = updatedUser;
    return Results.Ok(updatedUser);
})
.WithName("UpdateUserRole");

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
