using MassTransit;
using Products.Read.API.Abstractions;
using Products.Shared.Messages;

namespace Products.Read.API.MessageConsumers
{
    public class ProductDeletedConsumer : IConsumer<ProductDeletedMessage>
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductDeletedConsumer> _logger;

        public ProductDeletedConsumer(IProductRepository productRepository, ILogger<ProductDeletedConsumer> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<ProductDeletedMessage> context)
        {
            var message = context.Message;
            _logger.LogInformation("Product Deleted Message Received");

            bool success = await _productRepository.DeleteProductByAggregateIdAsync(message.AggregateId);
            _logger.LogInformation("Product successfully deleted from Products Read Side.");
        }
    }
}
