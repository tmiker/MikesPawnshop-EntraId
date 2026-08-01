using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Products.Write.API.ExceptionHandling.PendingIncorporation.Handlers
{
    public class CachedExceptionHandler : IExceptionHandler
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedExceptionHandler> _logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public CachedExceptionHandler(IMemoryCache cache, ILogger<CachedExceptionHandler> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"error_response_{exception.GetType().Name}_{httpContext.Request.Path}";

            if (!_cache.TryGetValue(cacheKey, out ProblemDetails? cachedProblemDetails))
            {
                cachedProblemDetails = CreateProblemDetails(httpContext, exception);
                _cache.Set(cacheKey, cachedProblemDetails, CacheDuration);
            }
            httpContext.Response.StatusCode = cachedProblemDetails?.Status ?? 500;
            await httpContext.Response.WriteAsJsonAsync(cachedProblemDetails, cancellationToken);

            return true;
        }
        private ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
        {
            // Implementation ...
            return new ProblemDetails();
        }
    }
}
