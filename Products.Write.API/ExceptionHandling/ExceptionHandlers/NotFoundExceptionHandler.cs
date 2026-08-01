using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Products.Write.Application.Exceptions;

namespace Products.Write.API.ExceptionHandling.ExceptionHandlers
{
    public class NotFoundExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<NotFoundExceptionHandler> _logger;
        public NotFoundExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<NotFoundExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not NotFoundException notFoundException) return false; // Exception not handled

            _logger.LogWarning("Resource not found: Exception Type: {Type} | {Message} | RequestId: {RequestId}", exception.GetType().FullName,  notFoundException.Message, httpContext.TraceIdentifier);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = "The requested resource was not found.",       // notFoundException.Message,
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
            //httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            //await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            //return true; // Exception handled

            // OPTION 2: USE PROBLEM DETAILS SERVICE TO HANDLE EXCEPTION
            // Ensure response status code is set
            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

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
