using MassTransit.Monitoring.Performance;
using Microsoft.EntityFrameworkCore;
using Products.Read.API.Abstractions;
using Products.Read.API.Domain.Models;
using Products.Read.API.Exceptions;
using Products.Read.API.Infrastructure.Data;
using Products.Shared.Messages;

namespace Products.Read.API.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductsReadDbContext _db;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ProductsReadDbContext db, ILogger<ProductRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task AddProductAsync(ProductAddedMessage message)
        {
            try
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
                bool success = await _db.SaveChangesAsync() > 0;
                if (success) return;
                // handle update error with no exception thrown
                else HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, null);
            }
            catch (Exception ex)    // likely a DbUpdateException
            {
                HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, ex);
            }
        }

        public async Task UpdateProductStatusAsync(StatusUpdatedMessage message)
        {
            try
            {
                // method GetCorrectProductAndVersionWithRetriesAsync handles null, missing versions, and duplicate messages
                Product? product = await GetCorrectProductAndVersionWithRetriesAsync(
                    message.GetType().Name, message.AggregateId, message.AggregateVersion, message.CorrelationId);

                product!.UpdateStatus(message.Status, message.AggregateVersion);
                bool success = await _db.SaveChangesAsync() > 0;

                // handle update error with no exception thrown
                if (!success) HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, null);
            }
            catch (DuplicateProductMessageException dupEx)
            {
                // would be thrown in call to GetCorrectProductAndVersionWithRetriesAsync() above
                // just log for info
                _logger.LogInformation(dupEx.Message);
            }
            catch (Exception ex)    // likely a DbUpdateException
            {
                HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, ex);
            }
        }

        public async Task AddProductImageAsync(ImageAddedMessage message)
        {
            try
            {
                // method GetCorrectProductAndVersionWithRetriesAsync handles null, missing versions, and duplicate messages
                Product? product = await GetCorrectProductAndVersionWithRetriesAsync(
                    message.GetType().Name, message.AggregateId, message.AggregateVersion, message.CorrelationId);

                ImageData image = new ImageData(message.Name!, message.Caption!, message.SequenceNumber, message.ImageUrl!, message.ThumbnailUrl!);
                product!.AddImage(image, message.AggregateVersion);
                bool success = await _db.SaveChangesAsync() > 0;

                // handle update error with no exception thrown
                if (!success) HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, null);

            }
            catch (DuplicateProductMessageException dupEx)
            {
                // just log for info
                _logger.LogInformation(dupEx.Message);
            }
            catch (Exception ex)    // likely a DbUpdateException
            {
                HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, ex);
            }
        }

        public async Task AddProductDocumentAsync(DocumentAddedMessage message)
        {
            try
            {
                // method GetCorrectProductAndVersionWithRetriesAsync handles null, missing versions, and duplicate messages
                Product? product = await GetCorrectProductAndVersionWithRetriesAsync(
                    message.GetType().Name, message.AggregateId, message.AggregateVersion, message.CorrelationId);

                DocumentData document = new DocumentData(message.Name!, message.Title!, message.SequenceNumber, message.DocumentUrl!);
                product!.AddDocument(document, message.AggregateVersion);
                // Console.WriteLine($"PRODUCT DOCUMENT COUNT AFTER ADD BEFORE SAVE CHANGES: {product.Documents!.Count}");
                bool success = await _db.SaveChangesAsync() > 0;
                // Console.WriteLine($"PRODUCT DOCUMENT COUNT AFTER ADD AND AFTER SAVE CHANGES: {product.Documents!.Count}");

                // handle update error with no exception thrown
                if (!success) HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, null);

            }
            catch (DuplicateProductMessageException dupEx)
            {
                // just log for info
                _logger.LogInformation(dupEx.Message);
            }
            catch (Exception ex)    // likely a DbUpdateException or MissingProductVersionException
            {
                HandleProductStateSynchronizationError(message.GetType().Name, message.AggregateId, message.CorrelationId!, ex);
            }
        }

        private async Task<Product?> GetCorrectProductAndVersionWithRetriesAsync(string messageType, Guid aggregateId, int messageVersion, string? correlationId) //, 
        {
            int intervalSeconds = 2;
            int retryCount = 3;
            int intervalMultiplier = 2;

            Product? product = null;
            while (retryCount > 0)
            {
                // get product from database
                product = await _db.Products.Include(p => p.Images).Include(p => p.Documents).AsSplitQuery().FirstOrDefaultAsync(p => p.AggregateId == aggregateId);

                if (product is not null)
                {
                    // if correct version found, return it
                    if (product.Version == messageVersion - 1) return product;
                    // if duplicate message, throw to break and clear call stack - will catch in process and log this as not a show stopper
                    else if (product.Version >= messageVersion) throw new DuplicateProductMessageException($"Duplicate message: Version {messageVersion}, AggregateId: {aggregateId}");
                    // if (product.Version < messageVersion - 1) continue to retry to see if prior message(s) arrive - i.e. don't break here
                }

                // requery database after a delay if the product is null or product.Version < (messageVersion - 1) to allow any new messages to be processed 
                // for retryCount = 3, intervalSeconds = 2, and intervalMultiplier = 2, delays will be 4s, then 8s, then 16s for a total of 28s
                intervalSeconds = intervalSeconds * intervalMultiplier;   
                retryCount--;
                await Task.Delay(intervalSeconds * 1000);
            }

            // handle null product result
            if (product is null) HandleProductIsNullSynchronizationError(messageType, aggregateId, correlationId!);
            // handle missing message version(s)
            else if (product.Version < messageVersion - 1) HandleMissingProductMessageVersionError(messageType, aggregateId, messageVersion, correlationId!);

            return product;
        }

        private void HandleProductIsNullSynchronizationError(string messageType, Guid aggregateId, string correlationId)
        {
            _logger.LogError("Error: Product associated with Write Side synchronization message was not found. " +
                "Message Type: {messageType}, AggregageId: {aggId}, CorrelationId: {corrId}.", messageType, aggregateId, correlationId);
            throw new DataConsistencyException($"Product associated with Write Side synchronization message was not found. " +
                $"Message Type: {messageType}, AggregageId: {aggregateId}, CorrelationId: {correlationId}");
        }

        private void HandleMissingProductMessageVersionError(string messageType, Guid aggregateId, int messageVersion, string correlationId)
        {
            _logger.LogError("Missing Product Message Version {missingMessageVersion}. Unable to process {messageType} message " +
                "for AggregateId {aggregateId}, Version {messageVersion}, CorrelationId {correlationId} as the previous message " +
                "is missing.", messageVersion - 1, messageType, aggregateId, messageVersion, correlationId);
            throw new DataConsistencyException($"Error synchronizing product state from write side message. " +
                $"Message Type: {messageType}, AggregageId: {aggregateId}, CorrelationId: {correlationId}");
        }

        private void HandleProductStateSynchronizationError(string messageType, Guid aggregateId, string correlationId, Exception? ex)
        {
            _logger.LogError("Error synchronizing product state from Write Side synchronization message. " +
                "Message Type: {messageType}, AggregageId: {aggId}, CorrelationId: {corrId}.  Exception: {ex}", messageType, aggregateId, correlationId, ex);
            // return -1;
            throw new DataConsistencyException($"Error synchronizing product state from write side message. " +
                $"Message Type: {messageType}, AggregageId: {aggregateId}, CorrelationId: {correlationId}");
        }

        public async Task<bool> PurgeAsync()
        {
            var images = await _db.ImageData.ToListAsync();
            var documents = await _db.DocumentData.ToListAsync();
            var products = await _db.Products.ToListAsync();
            var messageRecords = await _db.ProductMessageRecords.ToListAsync();
            _db.ImageData.RemoveRange(images);
            _db.DocumentData.RemoveRange(documents);
            _db.Products.RemoveRange(products);
            _db.ProductMessageRecords.RemoveRange(messageRecords);
            bool success = await _db.SaveChangesAsync() > 0;
            return success;
        }

        public async Task<bool> DeleteProductByAggregateIdAsync(Guid aggregateId)
        {
            Product? product = await _db.Products.FirstOrDefaultAsync(p => p.AggregateId == aggregateId);
            if (product is not null)
            {
                var images = await _db.ImageData.Where(i => i.ProductId == product.Id).ToListAsync();
                var documents = await _db.DocumentData.Where(d => d.ProductId == product.Id).ToListAsync();
                var messageRecords = await _db.ProductMessageRecords.Where(d => d.AggregateId == aggregateId).ToListAsync();
                if (images is not null && images.Any()) _db.ImageData.RemoveRange(images);
                if (documents is not null && documents.Any()) _db.DocumentData.RemoveRange(documents);
                if (messageRecords is not null && messageRecords.Any()) _db.ProductMessageRecords.RemoveRange(messageRecords);
                _db.Products.Remove(product);
                bool success = await _db.SaveChangesAsync() > 0;
                return success;
            }
            else return false;            
        }
    }
}
