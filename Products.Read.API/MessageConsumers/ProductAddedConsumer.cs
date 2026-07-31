using MassTransit;
using Products.Read.API.Abstractions;
using Products.Shared.Messages;

namespace Products.Read.API.MessageConsumers
{
    public class ProductAddedConsumer : IConsumer<ProductAddedMessage>
    {
        private readonly IProductMessageProcessor _messageProcessor;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductAddedConsumer> _logger;

        public ProductAddedConsumer(IProductMessageProcessor messageProcessor, IProductRepository productRepository, ILogger<ProductAddedConsumer> logger)
        {
            _messageProcessor = messageProcessor;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductAddedMessage> context)
        {
            var message = context.Message;
            _logger.LogInformation("Product Added Message Received: VERSION = {version}, AggregateId = {message.AggregateId}," +
                " Name = {message.Name}", message.AggregateVersion, message.AggregateId, message.Name);

            // note this will always return false for the Add Product Message
            bool messagesInMessageRecordQueue = await _messageProcessor.ProcessProductMessageAsync(message);

            if (messagesInMessageRecordQueue) await _messageProcessor.ProcessMessageRecordsFromQueue();
        }
    }
}
