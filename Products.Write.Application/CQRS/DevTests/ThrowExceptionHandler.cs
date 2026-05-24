using Azure;
using MediatR;
using Microsoft.Extensions.Logging;
using Products.Write.Application.Abstractions;
using Products.Write.Application.Exceptions;
using Products.Write.Infrastructure.Abstractions;
using Products.Write.Infrastructure.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace Products.Write.Application.CQRS.DevTests
{
    public class ThrowExceptionHandler : IRequestHandler<ThrowException, ThrowExceptionResult>
    {
        private readonly IProductRepository _productRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ThrowExceptionHandler> _logger;

        public ThrowExceptionHandler(IProductRepository productRepository, IEventAggregator eventAggregator, ILogger<ThrowExceptionHandler> logger)
        {
            _productRepository = productRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public async Task<ThrowExceptionResult> Handle(ThrowException command, CancellationToken cancellationToken)
        {
            
                if (command.CorrelationId is null) command.CorrelationId = Guid.NewGuid().ToString();

                await Task.Run(() => _logger.LogInformation("ThrowExceptionHandler will now throw an exception of type {exceptionType} " +
                    "with CorrelationId: {correlationId}", command.ExceptionType, command.CorrelationId));

                Exception ex = command.ExceptionType.ToLower() switch
                {
                    // handled by exception specific exception handler middleware
                    "producteventstoreexception" => throw new ProductEventStoreException("This is a test ProductEventStoreException thrown from ThrowExceptionHandler."),
                    "validationexception" => throw new ValidationException(        //"This is a test FluentValidation.ValidationException thrown from ThrowExceptionHandler"),
                        "This is a test FluentValidation.ValidationException thrown from ThrowExceptionHandler and should enumerate two validation errors.",
                        new List<ValidationFailure>()
                        {
                            new ValidationFailure("Error 1", "This is the first error"),
                            new ValidationFailure("Error 2", "This is the second error")
                        }),
                    "notfoundexception" => throw new NotFoundException("This is a test NotFoundException thrown from ThrowExceptionHandler."),

                    // handled by global exception handler middleware
                    "requestfailedexception" => throw new RequestFailedException("This is a test Azure RequestFailedException thrown from ThrowExceptionHandler."),
                    "unauthorizedaccessexception" => throw new UnauthorizedAccessException("This is a test UnauthorizedAccessException thrown from ThrowExceptionHandler."),
                    "forbiddenexception" => throw new ForbiddenException("This is a test ForbiddenException thrown from ThrowExceptionHandler."),
                    "conflictexception" => throw new ConflictException("This is a test ConflictException thrown from ThrowExceptionHandler."),
                    "argumentexception" => throw new ArgumentException("This is a test ArgumentException thrown from ThrowExceptionHandler."),
                    "argumentnullexception" => throw new ArgumentNullException("This is a test ArgumentNullException thrown from ThrowExceptionHandler."),
                    "invalidoperationexception" => throw new InvalidOperationException("This is a test InvalidOperationException thrown from ThrowExceptionHandler."),
                    "taskcanceledException" => throw new TaskCanceledException("This is a test TaskCanceledException thrown from ThrowExceptionHandler."),
                    _ => throw new ArgumentException("This is an ArgumentException thrown for an unsupported test exception type.")
                };

                return new ThrowExceptionResult(false, "An exception should have been thrown, so something went wrong.");
            
        }
    }
}