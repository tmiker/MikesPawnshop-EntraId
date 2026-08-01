using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Products.Write.Infrastructure.Exceptions;

namespace Products.Write.API.ExceptionHandling.ExceptionHandlers
{
    public class ProductEventStoreExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<ProductEventStoreExceptionHandler> _logger;
        public ProductEventStoreExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ProductEventStoreExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {            
            if (exception is not ProductEventStoreException productEventStoreException) return false; // Exception not handled

            _logger.LogWarning("Product Event Store Exception: Exception Type: {Type} | {Message} | RequestId: {RequestId}", exception.GetType().FullName, productEventStoreException.Message, httpContext.TraceIdentifier);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Product Event Store Error",
                Detail = "An error occurred in the Product Event Store.",       // productEventStoreException.Message,
                Instance = httpContext.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            };

            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
            problemDetails.Extensions["requestId"] = httpContext.TraceIdentifier;
            problemDetails.Extensions["machine"] = Environment.MachineName;
            // Include correlation ID if available
            problemDetails.Extensions["correlationId"] = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();

            //// OPTION 1: HANDLE EXCEPTION AND RETURN PROBLEM DETAILS OBJECT - NOTE WILL NOT HAVE CONTENT TYPE OF `application/problem+json`
            //httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            //await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            //return true; // Exception handled

            // OPTION 2: USE PROBLEM DETAILS SERVICE TO HANDLE EXCEPTION
            // Ensure response status code is set
            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status503ServiceUnavailable;

            // Use the Microsoft.AspNetCore.Http IProblemDetailsService to write the response
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });
        }
    }
}