using IPMS.Core.Domain.Users;
using Microsoft.Data.SqlClient;

namespace IPMS.Server.MiddleWare
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex,
                    "Domain rule violation. TraceId: {TraceId}",
                    context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = ex.Message,
                    traceId = context.TraceIdentifier
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized access. TraceId: {TraceId}",
                    context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "You are not allowed to perform this action.",
                    traceId = context.TraceIdentifier
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error occurred. TraceId: {TraceId}",
                    context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Database error occurred.",
                    traceId = context.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception. TraceId: {TraceId}",
                    context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unexpected server error.",
                    traceId = context.TraceIdentifier
                });
            }
        }
    }
}
