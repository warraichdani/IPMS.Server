using IPMS.Commands;
using IPMS.Core;
using IPMS.Core.Interfaces;
using IPMS.Core.Repositories;
using IPMS.Infrastructure;
using IPMS.Infrastructure.Repositories;
using IPMS.Models.DTOs;
using IPMS.Models.DTOs.Investments;
using IPMS.Models.Filters;
using IPMS.Queries.Allocation;
using IPMS.Queries.Dashboard;
using IPMS.Queries.Investments;
using IPMS.Queries.Performance;
using IPMS.Queries.Transactions;
using IPMS.Server.Extensions;
using IPMS.Server.Helpers;
using IPMS.Server.MiddleWare;
using IPMS.Server.Models;
using IPMS.Services;
using IPMS.Services.Investments;
using IPMS.Services.IPMS.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Add JWT authentication
services.AddAuthentication("Bearer")
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

        options.Events = new JwtBearerEvents
        {
            // THIS is where token expiration is detected
            OnAuthenticationFailed = async context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.NoResult();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var payload = new
                    {
                        error = "token_expired",
                        message = "Access token has expired"
                    };

                    await context.Response.WriteAsJsonAsync(payload);
                }
            },

            // Fallback for all other unauthorized cases
            OnChallenge = context =>
            {
                // Prevent default HTML response
                context.HandleResponse();

                // If response already written (e.g. token_expired), do nothing
                if (context.Response.HasStarted)
                    return Task.CompletedTask;

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var payload = new
                {
                    error = "unauthorized",
                    message = "Unauthorized access"
                };

                return context.Response.WriteAsJsonAsync(payload);
            }
        };
    });

services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173") // your React dev server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"));
});

services.AddAuthorization(options =>
{
    options.AddPolicy("OwnerPolicy", policy =>
        policy.RequireRole("Admin", "User"));
});

services.AddSingleton<IIPMSConfigService, IPMSConfigService>();
services.AddScoped(typeof(IEventLogger<>), typeof(EventLogger<>));

// ----------------------------
// Services Users
// ----------------------------
services.AddScoped<IUserService, UserService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
services.AddScoped<ITokenService, TokenService>();

// ----------------------------
// Repositories
// ----------------------------
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IInvestmentRepository, InvestmentRepository>();
services.AddScoped<ITransactionRepository, TransactionRepository>();
services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

// ----------------------------
// Unit of Work
// ----------------------------
services.AddScoped<IUnitOfWork, UnitOfWork>();

// ----------------------------
// Application Services (Commands)
// ----------------------------
services.AddScoped<IBuyInvestmentService, BuyInvestmentService>();
services.AddScoped<ISellInvestmentService, SellInvestmentService>();
services.AddScoped<IUpdatePriceService, UpdatePriceService>();
services.AddScoped<ICreateInvestmentService, CreateInvestmentService>();
services.AddScoped<IUpdateInvestmentService, UpdateInvestmentService>();
services.AddScoped<IDeleteInvestmentService, DeleteInvestmentService>();
services.AddScoped<IDeleteInvestmentsService, DeleteInvestmentsService>();

// ----------------------------
// Queries
// ----------------------------
services.AddScoped<IInvestmentListQuery, InvestmentListQuery>();
services.AddScoped<IInvestmentExportQuery, InvestmentExportQuery>();
services.AddScoped<ITransactionQuery, TransactionQuery>();
services.AddScoped<IInvestmentDetailQuery, InvestmentDetailQuery>();
services.AddScoped<IUserTransactionQuery, UserTransactionQuery>();

services.AddScoped<IUserDashboardQuery, UserDashboardQuery>();

//----Charts Dependencies starts----

services.AddScoped<IPerformanceQuery, PerformanceQuery>();
services.AddScoped<IAllocationQuery, AllocationQuery>();
services.AddScoped<IPortfolioPerformanceQuery, PortfolioPerformanceQuery>();

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

app.MapGet("/api/users", async (
    [AsParameters] UserListFilter filter,
           IUserService service) =>
    {
        var result = await service.GetAllAsync(filter);
        return Results.Ok(result);
    })
.RequireAuthorization("AdminPolicy");

app.MapDelete("/api/users/{id:guid}", async (Guid id, IUserService service) =>
{
    await service.SoftDeleteAsync(id);
    return Results.NoContent();
})
.RequireAuthorization("AdminPolicy");

app.MapPut("/api/users/{userId:guid}/toggle-active",
    async (Guid userId, IUserService service) =>
    {
        await service.ToggleActiveAsync(userId);
        return Results.NoContent();
    })
.RequireAuthorization("AdminPolicy");


//-------------------User DashBoard----------------------------------------

app.MapGet("/api/dashboard",
    (HttpContext ctx, IUserDashboardQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var dashboard = query.Get(userId.Value);
        return Results.Ok(dashboard);
    })
.RequireAuthorization("OwnerPolicy");


//-------------------Investment CRUD-----------------------------------

app.MapPost("/api/investments",
    (CreateInvestmentCommand cmd,
     HttpContext ctx,
     ICreateInvestmentService service) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var id = service.Execute(cmd, userId.Value);
        return Results.Created($"/investments/{id}", new { InvestmentId = id });
    })
.RequireAuthorization();

app.MapGet("/api/investments",
    ([AsParameters] InvestmentListFilter filter,
     HttpContext ctx,
     IInvestmentListQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        // Get investments for the logged-in user
        var result = query.Get(userId.Value, filter);

        // Return in a format suitable for React Query
        return Results.Ok(new
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    })
.RequireAuthorization();

app.MapPut("/api/investments/{id}",
    (Guid id,
     UpdateInvestmentCommand body,
     HttpContext ctx,
     IUpdateInvestmentService service) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        if (id != body.InvestmentId)
            return Results.BadRequest("InvestmentId mismatch.");

        service.Execute(body, userId.Value);

        return Results.NoContent();
    })
.RequireAuthorization();

app.MapGet("/api/investments/{id:guid}",
    ([FromRoute] Guid id,
     HttpContext ctx,
     IInvestmentDetailQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var investment = query.Get(id, userId.Value);
        if (investment is null)
            return Results.NotFound();

        return Results.Ok(investment);
    })
.RequireAuthorization();


app.MapGet("/api/investments/{id}/transactions",
    (Guid id,
     [AsParameters] TransactionListFilter filter,
     HttpContext ctx,
     ITransactionQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var transactions = query.GetTransactionsForInvestment(
            id,
            userId.Value,
            filter,
            out int totalCount);

        return Results.Ok(new
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    })
.RequireAuthorization();

//app.MapDelete("/api/investments/{id:guid}",
//    ([FromRoute] Guid id,
//     HttpContext ctx,
//     IDeleteInvestmentService service) =>
//    {
//        var userId = ctx.GetUserId();
//        if (userId is null)
//            return Results.Unauthorized();

//        service.Execute(id, userId.Value);
//        return Results.NoContent();
//    })
//.RequireAuthorization();

//app.MapDelete("/investments",
//    ([AsParameters] DeleteInvestmentsRequest request,
//     HttpContext ctx,
//     IDeleteInvestmentsService service) =>
//    {
//        var userId = ctx.GetUserId();
//        if (userId is null)
//            return Results.Unauthorized();

//        service.Execute(new DeleteInvestmentsCommand(
//            request.InvestmentIds,
//            userId.Value));

//        return Results.NoContent();
//    })
//.RequireAuthorization();


////----------------Investments Export to CSV----------------------

app.MapGet("/api/investments/export",
    ([AsParameters] InvestmentListFilter filter,
     HttpContext ctx,
     IInvestmentExportQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var data = query.Export(userId.Value, filter);

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

////----------------Investments Behaviour----------------------

app.MapPost("/api/investments/buy",
    (BuyInvestmentCommand cmd,
     HttpContext ctx,
     IBuyInvestmentService service) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var response = service.Execute(cmd with { UserId = userId.Value });
        return Results.Ok(response);
    })
.RequireAuthorization();

app.MapPost("api/investments/sell", async (
    SellInvestmentCommand cmd,
    HttpContext ctx,
    ISellInvestmentService sellService) =>
{
    var userId = ctx.GetUserId();
    if (userId is null)
        return Results.BadRequest("Invalid user id in token.");

    var transactionDto = sellService.Execute(cmd with { UserId = userId.Value });

    return Results.Ok(transactionDto);
}).RequireAuthorization();

app.MapPost("/api/investments/update-price",
    (UpdatePriceCommand cmd,
     HttpContext ctx,
     IUpdatePriceService service) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var response = service.Execute(cmd with { UserId = userId.Value });
        return Results.Ok(response);
    })
.RequireAuthorization();

////--------------Transactions-----------------------

app.MapGet("/api/transactions",
    (HttpContext ctx,
     [AsParameters] AllTransactionListFilter filter,
     IUserTransactionQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        var result = query.Get(userId.Value, filter);

        return Results.Ok(new
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    })
.RequireAuthorization();

////--------------Charts-----------------------------
app.MapGet("/api/investments/{id}/performance",
    (HttpContext ctx, Guid id, IPerformanceQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        return Results.Ok(query.GetLast12Months(id, userId.Value));
    }).RequireAuthorization();

app.MapGet("/api/portfolio/allocation",
    (HttpContext ctx, IAllocationQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        return Results.Ok(query.GetByUser(userId.Value));
    }).RequireAuthorization();


app.MapGet("/api/dashboard/performance",
    (HttpContext ctx, IPortfolioPerformanceQuery query) =>
    {
        var userId = ctx.GetUserId();
        if (userId is null)
            return Results.Unauthorized();

        return Results.Ok(query.GetLast12Months(userId.Value));
    })
.RequireAuthorization();

app.Run();
