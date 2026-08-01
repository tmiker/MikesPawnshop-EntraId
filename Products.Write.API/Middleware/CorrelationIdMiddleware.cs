using Products.Write.Application.Extensions;

namespace Products.Write.API.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //// uncomment to prevent log chattiness
            if (context.Request.Path == "/api/productsManagement/health") return; 

            // Check if Correlation ID exists in the request header
            bool presentInRequestHeader = true;

            if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            {
                // Generate a new Correlation ID if not present
                presentInRequestHeader = false;
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers["X-Correlation-ID"] = correlationId;
            }

            // Add Correlation ID to the response header
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            _logger.LogCorrelationIdMiddleware(context.Request.Method, context.Request.Path, correlationId, presentInRequestHeader); //, DateTime.Now);

            // Proceed to the next middleware
            await _next(context);
        }
    }
}