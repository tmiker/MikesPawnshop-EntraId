using MassTransit;
using Products.Read.API.Abstractions;
using Products.Shared.Messages;

namespace Products.Read.API.MessageConsumers
{
    public class StatusUpdateConsumer : IConsumer<StatusUpdatedMessage>
    {
        private readonly IProductMessageProcessor _messageProcessor;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<StatusUpdateConsumer> _logger;

        public StatusUpdateConsumer(IProductMessageProcessor messageProcessor, IProductRepository productRepository, ILogger<StatusUpdateConsumer> logger)
        {
            _messageProcessor = messageProcessor;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StatusUpdatedMessage> context)
        {
            var message = context.Message;
            _logger.LogInformation("Status Updated Message Received: VERSION = {version}, AggregateId = {message.AggregateId}, " +
                "Status = {message.Status}", message.AggregateVersion, message.AggregateId, message.Status);

            bool messagesInMessageRecordQueue = await _messageProcessor.ProcessProductMessageAsync(message);

            if (messagesInMessageRecordQueue) await _messageProcessor.ProcessMessageRecordsFromQueue();
        }
    }
}
