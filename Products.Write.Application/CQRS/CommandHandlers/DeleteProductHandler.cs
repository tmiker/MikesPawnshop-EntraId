using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Products.Shared.Messages;
using Products.Write.Application.CQRS.DevTests;
using Products.Write.Domain.Aggregates;
using Products.Write.Domain.Snapshots;
using Products.Write.Infrastructure.Abstractions;

namespace Products.Write.Application.CQRS.CommandHandlers
{
    public class DeleteProductHandler : IRequestHandler<DeleteProduct, DeleteProductResult>
    {
        private readonly IProductRepository _productRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<DeleteProductHandler> _logger;

        public DeleteProductHandler(IProductRepository productRepository, IPublishEndpoint publishEndpoint, ILogger<DeleteProductHandler> logger)
        {
            _productRepository = productRepository;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<DeleteProductResult> Handle(DeleteProduct command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.CorrelationId)) command.CorrelationId = Guid.NewGuid().ToString();

            Product product = await _productRepository.GetProductByIdAsync(command.AggregateId);
            if (product is null) return new DeleteProductResult(false, $"A product with Id {command.AggregateId} was not found.");

            ProductSnapshot snapshot = product.GetSnapshot();
            bool success = await _productRepository.DeleteProductByIdAsync(command.AggregateId);

            if (success)
            {
                _logger.LogInformation("Product successfully purged from Write Side Products.");
                
                // bypassing domain, so just create and publish the message
                ProductDeletedMessage deleteMessage = new ProductDeletedMessage(command.AggregateId, product.GetType().Name, snapshot.Version, command.CorrelationId);

                await _publishEndpoint.Publish(deleteMessage);

                return new DeleteProductResult(true, null);
            }
            else return new DeleteProductResult(false, "An error occurred while deleting the product from Write Side Products.");
        }
    }
}
