using IPMS.Core.Interfaces;
using IPMS.DTOs;
using IPMS.Infrastructure.Repositories;
using IPMS.Server.MiddleWare;
using IPMS.Server.Models;
using IPMS.Services;
using IPMS.Services.IPMS.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add JWT authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173") // your React dev server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OwnerPolicy", policy =>
        policy.RequireRole("Admin", "User"));
});

builder.Services.AddSingleton<IIPMSConfigService, IPMSConfigService>();

// Register DI
builder.Services.AddScoped<IUserRepository>(sp =>
    new UserRepository(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped(typeof(IEventLogger<>), typeof(EventLogger<>));


var app = builder.Build();
app.UseCors("AllowFrontend");

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/configurations", (IIPMSConfigService service) =>
{
    return Results.Ok(service.GetConfigs());
});

app.MapPost("/api/users/register", async (RegisterUserDto dto, IUserService service) =>
{
    var result = await service.RegisterAsync(dto);
    return Results.Ok(result);
});

app.MapPost("/api/auth/confirm-email", async (ConfirmEmailRequest request, IEmailConfirmationService service) =>
{
    var success = await service.ConfirmEmailAsync(request.Email, request.Otp);
    return success ? Results.Ok(new { message = "Email confirmed successfully." })
                   : Results.BadRequest(new { message = "Invalid or expired OTP." });
});

app.MapPost("/api/auth/login", async (LoginRequestDto dto, IAuthService authService) =>
{
    var result = await authService.LoginAsync(dto);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapPost("/api/auth/refresh", async (string refreshToken, IAuthService authService) =>
{
    var result = await authService.RefreshAsync(refreshToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapGet("/api/users", async (IUserService service) =>
{
    var users = await service.GetAllAsync();
    return Results.Ok(users);
})
.RequireAuthorization("AdminPolicy");

app.MapDelete("/api/users/{id:guid}", async (Guid id, IUserService service) =>
{
    await service.SoftDeleteAsync(id);
    return Results.NoContent();
})
.RequireAuthorization("AdminPolicy");

app.Run();
