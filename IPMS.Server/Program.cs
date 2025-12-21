using IPMS.Core.Interfaces;
using IPMS.DTOs;
using IPMS.Infrastructure.Repositories;
using IPMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Register DI
builder.Services.AddScoped<IUserRepository>(sp =>
    new UserRepository(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.MapPost("/api/users/register", async (RegisterUserDto dto, IUserService service) =>
{
    var result = await service.RegisterAsync(dto);
    return Results.Ok(result);
});

app.MapPost("/api/users/login", async (LoginUserDto dto, IUserService service) =>
{
    var result = await service.LoginAsync(dto);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapGet("/api/users", async (IUserService service) =>
{
    var users = await service.GetAllAsync();
    return Results.Ok(users);
});

app.MapDelete("/api/users/{id:guid}", async (Guid id, IUserService service) =>
{
    await service.SoftDeleteAsync(id);
    return Results.NoContent();
});

app.Run();
