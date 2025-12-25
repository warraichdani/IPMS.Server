using IPMS.Commands;
using IPMS.Core.Interfaces;
using IPMS.DTOs;
using IPMS.DTOs.Investments;
using IPMS.Infrastructure.Repositories;
using IPMS.Queries.Allocation;
using IPMS.Queries.Investments;
using IPMS.Queries.Performance;
using IPMS.Server.Extensions;
using IPMS.Server.Helpers;
using IPMS.Server.MiddleWare;
using IPMS.Server.Models;
using IPMS.Services;
using IPMS.Services.Investments;
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

//----Charts Dependencies starts----

builder.Services.AddScoped<IPerformanceQuery>(sp =>
    new PerformanceQuery(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAllocationQuery>(sp =>
    new AllocationQuery(builder.Configuration.GetConnectionString("DefaultConnection")));

//----Charts Dependencies End-------
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

app.MapPost("/api/auth/logout", async (
    HttpContext httpContext,
    LogoutRequest request,
    IAuthService authService) =>
{
    var userId = httpContext.GetUserId();
    if (userId == null)
        return Results.BadRequest("Invalid user id in token.");

    await authService.LogoutAsync(userId.Value, request.RefreshToken);
    return Results.Ok();
})
.RequireAuthorization();

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
//-------------------Investment CRUD-----------------------------------

app.MapPost("/investments",
    (CreateInvestmentCommand cmd,
     CurrentUser user,
     ICreateInvestmentService service) =>
    {
        if (user is null) return Results.Unauthorized();

        var id = service.Execute(cmd, user.UserId);
        return Results.Created($"/investments/{id}", new { InvestmentId = id });
    })
.RequireAuthorization();

app.MapPut("/investments/{id}",
    (Guid id,
     UpdateInvestmentCommand body,
     CurrentUser user,
     IUpdateInvestmentService service) =>
    {
        if (user is null)
            return Results.Unauthorized();

        if (id != body.InvestmentId)
            return Results.BadRequest("InvestmentId mismatch.");

        service.Execute(body, user.UserId);

        return Results.NoContent();
    })
.RequireAuthorization();

//----------------Investments Export to CSV----------------------

app.MapGet("/investments/export",
    (InvestmentListFilter filter,
     CurrentUser user,
     IInvestmentExportQuery query) =>
    {
        if (user is null)
            return Results.Unauthorized();

        var data = query.Export(user.UserId, filter);

        var csv = CsvWriter.Write(
            data,
            new[]
            {
            "Name",
            "Type",
            "Amount",
            "Current Value",
            "Gain/Loss %",
            "Date",
            "Status"
            },
            r => new[]
            {
            r.InvestmentName,
            r.InvestmentType,
            r.Amount.ToString("0.00"),
            r.CurrentValue.ToString("0.00"),
            r.GainLossPercent.ToString("0.00"),
            r.PurchaseDate.ToString("yyyy-MM-dd"),
            r.Status
            });

        return Results.File(
            csv,
            "text/csv",
            $"investments_{DateTime.UtcNow:yyyyMMdd}.csv");
    })
.RequireAuthorization();

//----------------Investments Behaviour----------------------

app.MapPost("/investments/buy",
    (BuyInvestmentCommand cmd,
     CurrentUser user,
     IBuyInvestmentService service) =>
    {
        if (user is null)
            return Results.Unauthorized();

        var response = service.Execute(cmd with { UserId = user.UserId });
        return Results.Ok(response);
    })
.RequireAuthorization();

app.MapPost("api/investments/sell", async (
    SellInvestmentCommand cmd,
    CurrentUser user,
    ISellInvestmentService sellService) =>
{
    if (user == null)
        return Results.BadRequest("Invalid user id in token.");

    var transactionDto = sellService.Execute(cmd with { UserId = user.UserId });

    return Results.Ok(transactionDto);
}).RequireAuthorization();

app.MapPost("/investments/update-price",
    (UpdatePriceCommand cmd,
     CurrentUser user,
     IUpdatePriceService service) =>
    {
        if (user is null)
            return Results.Unauthorized();

        var response = service.Execute(cmd with { UserId = user.UserId });
        return Results.Ok(response);
    })
.RequireAuthorization();


//--------------Charts-----------------------------
app.MapGet("/investments/{id}/performance",
    (CurrentUser user, Guid id, IPerformanceQuery query) =>
    {
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(query.GetLast12Months(id, user.UserId));
    }).RequireAuthorization();

app.MapGet("/portfolio/allocation",
    (CurrentUser user, IAllocationQuery query) =>
    {
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(query.GetByUser(user.UserId));
    }).RequireAuthorization();


app.Run();
