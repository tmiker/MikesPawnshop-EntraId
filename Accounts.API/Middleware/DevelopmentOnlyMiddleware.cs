namespace Accounts.API.Middleware
{
    public class DevelopmentOnlyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DevelopmentOnlyMiddleware> _logger;

        public DevelopmentOnlyMiddleware(RequestDelegate next, ILogger<DevelopmentOnlyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //if (context.Request.Headers.TryGetValue("Bearer", out var token))
            //{
            //    if (!string.IsNullOrWhiteSpace(token)) _logger.LogInformation("\n************** DevelopmentOnlyMiddleware - Bearer Token: ******** \n{Token}\n", token.ToString());
            //    else _logger.LogInformation("\n************** DevelopmentOnlyMiddleware - No Bearer Token found in request headers. ********\n");
            //}

            // Proceed to the next middleware
            await _next(context);
        }
    }
}
