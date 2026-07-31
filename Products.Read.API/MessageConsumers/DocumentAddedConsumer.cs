using MassTransit;
using Products.Read.API.Abstractions;
using Products.Shared.Messages;

namespace Products.Read.API.MessageConsumers
{
    public class DocumentAddedConsumer : IConsumer<DocumentAddedMessage>
    {
        private readonly IProductMessageProcessor _messageProcessor;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<DocumentAddedConsumer> _logger;

        public DocumentAddedConsumer(IProductMessageProcessor messageProcessor, IProductRepository productRepository, ILogger<DocumentAddedConsumer> logger)
        {
            _messageProcessor = messageProcessor;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<DocumentAddedMessage> context)
        {
            var message = context.Message;
            _logger.LogInformation("Document Added Message Received: VERSION = {version}, AggregateId = {message.AggregateId}, " +
                "Title = {message.Title}", message.AggregateVersion, message.AggregateId, message.Title);

            bool messagesInMessageRecordQueue = await _messageProcessor.ProcessProductMessageAsync(message);

            if (messagesInMessageRecordQueue) await _messageProcessor.ProcessMessageRecordsFromQueue();
        }
    }
}
