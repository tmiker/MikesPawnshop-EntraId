namespace Products.Read.API.Extensions
{
    public static partial class HighPerformanceLoggingExtensions
    {
        [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Health Check Status: {status}")]
        public static partial void LogHealthCheckStatus(this ILogger logger, string? status);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Request Method: {method}, Request Path: {path}, CorrelationId: {correlationId}, Found in Request Header: {presentInRequestHeader}")]
        public static partial void LogCorrelationIdMiddleware(this ILogger logger, string? method, string? path, string? correlationId, bool presentInRequestHeader);  

    }
}
