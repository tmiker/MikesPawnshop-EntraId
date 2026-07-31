using Microsoft.AspNetCore.OutputCaching;

namespace Products.Read.API.CachePolicies
{
    public sealed class CustomOutputCachePolicy : IOutputCachePolicy
    {
        public static readonly CustomOutputCachePolicy Instance = new();

        private CustomOutputCachePolicy() { }

        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken ct)
        {
            var attemptOutputCaching = AttemptOutputCaching(context);
            context.EnableOutputCaching = true;
            context.AllowCacheLookup = attemptOutputCaching;
            context.AllowCacheStorage = attemptOutputCaching;
            context.AllowLocking = true;
            context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(60);
            // Vary by query by defaults
            context.CacheVaryByRules.QueryKeys = "*";
            context.Tags.Add("products");
            return ValueTask.CompletedTask;
        }

        private bool AttemptOutputCaching(OutputCacheContext context)
        {
            // Check if the current request fulfills the requirements to be cached

            var request = context.HttpContext.Request;

            // Verify the method
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            {
                return false;
            }

            // Always allow caching if Authorization header is present
            // Verify existence of authorization headers
            //if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || request.HttpContext.User?.Identity?.IsAuthenticated == true)
            //{
            //    return false;
            //}

            return true;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            var response = context.HttpContext.Response;
            context.AllowCacheStorage = true;

            return ValueTask.CompletedTask;
        }
    }
}
