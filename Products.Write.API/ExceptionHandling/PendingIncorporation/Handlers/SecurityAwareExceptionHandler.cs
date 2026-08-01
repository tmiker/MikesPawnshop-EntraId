using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Products.Write.Application.Exceptions;
using System.Security;

namespace Products.Write.API.ExceptionHandling.PendingIncorporation.Handlers
{
    public class SecurityAwareExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<SecurityAwareExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;
        // private readonly SecuritySettings _securitySettings;  // requires nuget: DotNetOpenAuth.OAuth.Core or DotNetOpenAuth.OpenId.Core

        public SecurityAwareExceptionHandler(ILogger<SecurityAwareExceptionHandler> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Log security-relevant information
            if (IsSecurityException(exception))
            {
                _logger.LogWarning("Security exception: {ExceptionType} | IP: {RemoteIp} | User: {User} | Path: {Path}",
                    exception.GetType().Name,
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    httpContext.User?.Identity?.Name ?? "Anonymous",
                    httpContext.Request.Path.Value);
            }
            var problemDetails = CreateSecureProblemDetails(httpContext, exception);

            // Add security headers
            httpContext.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            httpContext.Response.Headers.Append("X-Frame-Options", "DENY");

            httpContext.Response.StatusCode = problemDetails.Status ?? 500;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private bool IsSecurityException(Exception exception)
        {
            return exception is UnauthorizedAccessException or SecurityException or ForbiddenException;
        }

        private ProblemDetails CreateSecureProblemDetails(HttpContext httpContext, Exception exception)
        {
            var (statusCode, title, detail) = GetSecureErrorInfo(exception);

            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = _environment.IsDevelopment() ? exception.Message : detail,
                Instance = httpContext.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["requestId"] = httpContext.TraceIdentifier
                    // Don't include sensitive debugging information in production
                }
            };
        }
        private static (int statusCode, string title, string detail) GetSecureErrorInfo(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => (401, "Unauthorized", "Authentication required"),
                ForbiddenException => (403, "Forbidden", "Access denied"),
                SecurityException => (403, "Security Error", "Security policy violation"),
                _ => (500, "Internal Server Error", "An error occurred")
            };
        }
    }
}
