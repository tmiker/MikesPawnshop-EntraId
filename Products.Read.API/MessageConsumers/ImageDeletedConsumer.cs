using MassTransit;
using Products.Read.API.Abstractions;
using Products.Shared.Messages;

namespace Products.Read.API.MessageConsumers
{
    public class ImageDeletedConsumer : IConsumer<ImageDeletedMessage>
    {
        private readonly IProductMessageProcessor _messageProcessor;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ImageDeletedConsumer> _logger;

        public ImageDeletedConsumer(IProductMessageProcessor messageProcessor, IProductRepository productRepository, ILogger<ImageDeletedConsumer> logger)
        {
            _messageProcessor = messageProcessor;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ImageDeletedMessage> context)
        {
            var message = context.Message;
            _logger.LogInformation("Image Deleted Message Received: VERSION = {version}, AggregateId = {message.AggregateId}, " +
                "Title = {message.Title}", message.AggregateVersion, message.AggregateId, message.FileName);

            bool messagesInMessageRecordQueue = await _messageProcessor.ProcessProductMessageAsync(message);

            // really want to batch process messages and call the below after processing a batch, or something equivalent
            if (messagesInMessageRecordQueue) await _messageProcessor.ProcessMessageRecordsFromQueue();
        }
    }
}
