using MediatR;
using Microsoft.Extensions.Logging;
using Products.Write.Application.Abstractions;
using Products.Write.Application.CQRS.CommandResults;
using Products.Write.Application.CQRS.Commands;
using Products.Write.Domain.Aggregates;
using Products.Write.Infrastructure.Abstractions;

namespace Products.Write.Application.CQRS.CommandHandlers
{
    public class DeleteImageHandler : IRequestHandler<DeleteImage, DeleteImageResult>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAzureStorageService _azureStorageService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<DeleteImageHandler> _logger;

        public DeleteImageHandler(IProductRepository productRepository, IAzureStorageService azureStorageService, IEventAggregator eventAggregator, ILogger<DeleteImageHandler> logger)
        {
            _productRepository = productRepository;
            _azureStorageService = azureStorageService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public async Task<DeleteImageResult> Handle(DeleteImage command, CancellationToken cancellationToken)
        {
            try
            {
                Product product = await _productRepository.GetProductByIdAsync(command.ProductId);

                // delete image in domain
                product.DeleteImage(command.FileName, command.CorrelationId);
                bool success = await _productRepository.SaveAsync(product);

                if (success)
                {
                    // invoke domain events to update read side data store
                    if (product.DomainEvents is not null && product.DomainEvents.Any())
                    {
                        foreach (var domainEvent in product.DomainEvents)
                        {
                            // publish the event to the bus
                            _eventAggregator.Raise(domainEvent);
                        }
                    }

                    // delete image from azure blob storage
                    string containerName = $"product-{command.ProductId}";
                    string fileName = command.FileName;
                    (bool IsSuccess, string? ErrorMessage) result = await _azureStorageService.DeleteProductImageFromAzureAsync(containerName, fileName, cancellationToken);
                    return new DeleteImageResult(result.IsSuccess, result.ErrorMessage);
                }

                return new DeleteImageResult(success, "Error removing image from write side data store");
            }
            catch (Exception ex)
            {
                return new DeleteImageResult(false, ex.Message);
            }
        }
    }
}