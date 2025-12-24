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
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error occurred. TraceId: {TraceId}",
                    context.TraceIdentifier);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Database error occurred"
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
                    message = "Unexpected server error"
                });
            }
        }
    }
}
