using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Products.Read.API.Abstractions;
using Products.Read.API.Domain.Models;
using Products.Read.API.Exceptions;
using Products.Read.API.Infrastructure.Data;
using Products.Shared.Messages;
using Products.Shared.Abstractions;

namespace Products.Read.API.MessageServices
{
    public class ProductMessageProcessor : IProductMessageProcessor
    {
        private readonly ProductsReadDbContext _db;
        private readonly ILogger<ProductMessageProcessor> _logger;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None };

        public ProductMessageProcessor(ProductsReadDbContext db, ILogger<ProductMessageProcessor> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> ProcessProductMessageAsync(IProductMessage message)
        {
            bool messagesInMessageRecordQueue = false;
            try
            {
                if (message is ProductAddedMessage productAddedMessage) ProcessNewProductMessage(productAddedMessage);
                else
                {
                    Product? product = await _db.Products.FirstOrDefaultAsync(p => p.AggregateId == message.AggregateId);
                    if (product is not null && product.Version >= message.AggregateVersion) return false;   // duplicate message - idempotency 1
                    if (product is not null && product.Version == message.AggregateVersion - 1) ProcessExistingProductMessage(product, (dynamic)message);
                    else
                    {
                        SaveAsMessageRecord(message, false);
                        messagesInMessageRecordQueue = true;
                    }
                }
                await _db.SaveChangesAsync();
                return messagesInMessageRecordQueue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex.InnerException?.Message);
                HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, ex);
                return messagesInMessageRecordQueue;
            }
        }

        private void SaveAsMessageRecord(IProductMessage message, bool isProcessed)
        {
            ProductMessageRecord record = new ProductMessageRecord(
                    message.AggregateId,
                    message.AggregateVersion,
                    message.GetType().AssemblyQualifiedName ?? throw new InvalidDataException("Invalid Event Type"),
                    JsonConvert.SerializeObject(message, _serializerSettings),
                    message.CorrelationId);
            if (isProcessed) record.IsProcessed = true;

            _db.ProductMessageRecords.Add(record);
        }

        private void ProcessNewProductMessage(ProductAddedMessage message)
        {
            Product product = new Product
                (
                    aggregateId: message.AggregateId,
                    name: message.Name,
                    category: message.Category,
                    description: message.Description,
                    price: message.Price,
                    currency: message.Currency,
                    status: message.Status,
                    quantityOnHand: message.QuantityOnHand,
                    quantityAllocated: message.QuantityAllocated,
                    uom: message.UOM,
                    lowStockThreshold: message.LowStockThreshold,
                    version: message.AggregateVersion
                );

            _db.Products.Add(product);
        }

        private void ProcessExistingProductMessage(Product product, StatusUpdatedMessage message)
        {
            product!.UpdateStatus(message.Status, message.AggregateVersion);
        }

        private void ProcessExistingProductMessage(Product product, ImageAddedMessage message)
        {
            ImageData image = new ImageData(message.Name!, message.Caption!, message.SequenceNumber, message.ImageUrl!, message.ThumbnailUrl!);
            product!.AddImage(image, message.AggregateVersion);
        }

        private void ProcessExistingProductMessage(Product product, DocumentAddedMessage message)
        {
            DocumentData document = new DocumentData(message.Name!, message.Title!, message.SequenceNumber, message.DocumentUrl!);
            product!.AddDocument(document, message.AggregateVersion);
        }

        private void ProcessExistingProductMessage(Product product, ImageDeletedMessage message)
        {
            product!.DeleteImage(message.FileName, message.AggregateVersion);
        }

        private void ProcessExistingProductMessage(Product product, DocumentDeletedMessage message)
        {
            product!.DeleteDocument(message.FileName, message.AggregateVersion);
        }

        private void HandleProductStateSynchronizationError(string messageType, Guid aggregateId, string correlationId, Exception? ex)
        {
            _logger.LogError("Error synchronizing product state from Write Side synchronization message. " +
                "Message Type: {messageType}, AggregageId: {aggId}, CorrelationId: {corrId}.  Exception: {ex}", messageType, aggregateId, correlationId, ex);
            // return -1;
            throw new DataConsistencyException($"Error synchronizing product state from write side message. " +
                $"Message Type: {messageType}, AggregageId: {aggregateId}, CorrelationId: {correlationId}");
        }

        public async Task ProcessMessageRecordsFromQueue()
        {
            // get all message records
            List<ProductMessageRecord> records = await _db.ProductMessageRecords.ToListAsync();
            // get unique ids for the records so can process by product
            List<Guid> aggregateIds = records.DistinctBy(r => r.AggregateId).Select(r => r.AggregateId).ToList();
            // process message records by product
            foreach (Guid id in aggregateIds)
            {
                // get the product object to apply the message record content
                Product? product = await _db.Products.FirstOrDefaultAsync(p => p.AggregateId == id);
                // get the message records for this product and order them by version
                List<ProductMessageRecord> messageRecords = records.Where(r => r.AggregateId == id).OrderBy(r => r.AggregateVersion).ToList();
                // for each mesage record, if the version supercedes the current product version by 1, it is the correct record and should be processed if not already processed
                foreach (var messageRecord in messageRecords)
                {
                    if (messageRecord.AggregateVersion == product?.Version + 1 && !messageRecord.IsProcessed)
                    {
                        // deserialize the record message payload to get the message
                        IProductMessage message = (IProductMessage)JsonConvert.DeserializeObject(messageRecord.MessageJson, Type.GetType(messageRecord.MessageType)!)!;
                        // call the domain method of the product to apply the message
                        ProcessExistingProductMessage(product, (dynamic)message);
                        // mark the record processed
                        messageRecord.IsProcessed = true;
                    }
                    else if (messageRecord.AggregateVersion <= product?.Version) messageRecord.IsProcessed = true;      // duplicate message - idempotency 2
                }
            }

            // get processed records and remove them
            IEnumerable<ProductMessageRecord> processedRecords = records.Where(r => r.IsProcessed == true);
            _db.ProductMessageRecords.RemoveRange(processedRecords);
            await _db.SaveChangesAsync();
        }
    }
}
