using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;
using Products.Write.Domain.Aggregates;
using Products.Write.Domain.Base;
using Products.Write.Domain.Snapshots;
using Products.Write.Infrastructure.Abstractions;

namespace Products.Write.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IProductEventStore _eventStore;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(IProductEventStore eventStore, ILogger<ProductRepository> logger)
        {
            _eventStore = eventStore;
            _logger = logger;
        }

        // EVENT RECORDS
        public async Task<bool> SaveAsync(Product product)
        {
            bool needsSnapshotUpdate = false;

            if (product.DomainEvents != null && product.DomainEvents.Any())
            {
                //// determine if need a snapshot if take every 10 versions
                int count = product.DomainEvents.Count(e => e.AggregateVersion > 0 && e.AggregateVersion % 10 == 0);
                if (count > 0) needsSnapshotUpdate = true;
                Console.WriteLine($"ProductRepository needsSnapshotUpdate value = {needsSnapshotUpdate}");

                // save as event records
                bool success = await _eventStore.SaveEventRecordsAsync(product.DomainEvents);

                // if needs snapshot save snapshot
                if (needsSnapshotUpdate) await _eventStore.SaveAsSnapshotRecordAsync(product.GetSnapshot());

                return success; // error handling occurs in event store
            }
            else
            {
                _logger.LogInformation("Product Repository found no events to send to the Event Store for Product Id: {productId}", product.Id);
                return true; // nothing to save, but not an error
            }
        }

        public async Task<Product> GetProductByIdAsync(Guid aggregateId)
        {
            IEnumerable<IDomainEvent> events = await _eventStore.GetDomainEventsByIdAsync(aggregateId, 0, Int32.MaxValue);
            Product product = new Product(events);
            return product;
        }

        public async Task<Product> GetProductByIdAndVersionAsync(Guid aggregateId, int minVersion, int maxVersion)
        {
            IEnumerable<IDomainEvent> events = await _eventStore.GetDomainEventsByIdAsync(aggregateId, minVersion, maxVersion);
            Product product = new Product(events);
            return product;
        }

        // SNAPSHOT RECORDS
        private async Task<bool> SaveSnapshotRecordAsync(Product product)
        {
            ProductSnapshot snapshot = product.GetSnapshot();
            var success = await _eventStore.SaveAsSnapshotRecordAsync(snapshot);
            return success;
        }

        public async Task<Product?> GetProductByIdUsingSnapshotsAsync(Guid aggregateId)
        {
            // if a snapshot is available use that
            ProductSnapshot? snapshot = await _eventStore.GetProductSnapshotAsync(aggregateId);
            if (snapshot is not null)
            {
                _logger.LogInformation("Product repository found ProductSnapshot with version {snapshot.Version}", snapshot.Version);
                // if last event version that is contained in snapshot is equal to the snapshot version
                IEnumerable<IDomainEvent> domainEvents = await _eventStore.GetDomainEventsByIdAsync(aggregateId, snapshot.Version + 1, int.MaxValue);
                //// if last event version that is contained in the snapshot is one less than the snapshot version - so need to get events with versions = snapshot version and greater
                //IEnumerable<IDomainEvent> domainEvents = await _eventStore.GetDomainEventsByIdAsync(aggregateId, snapshot.Version, int.MaxValue);

                Product product = new Product(snapshot);
                if (domainEvents.Any())
                {
                    domainEvents.OrderBy(d => d.AggregateVersion).ToList();
                    _logger.LogInformation($"Product repository retrieved {domainEvents.Count()} domain events with versions from {domainEvents.First().AggregateVersion} to {domainEvents.Last().AggregateVersion}");

                    foreach (var @event in domainEvents)
                    {
                        product.Apply(@event);
                        _logger.LogInformation($"Project repository applied domain event of type {@event.GetType().Name}, version {@event.AggregateVersion}");
                    }
                }
                return product;
            }
            else
            {
                // case no snapshot is found
                IEnumerable<IDomainEvent> domainEvents = await _eventStore.GetDomainEventsByIdAsync(aggregateId, 0, int.MaxValue);

                if (domainEvents.Any())
                {
                    Product product = new Product(domainEvents);
                    return product;
                }
                return null;
            }
        }

        // OUTBOX


        // DEV / ADMIN ONLY
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            IEnumerable<Guid> uniqueAggregateIds = await _eventStore.GetUniqueAggregateIdsAsync();
            ICollection<Product> products = new List<Product>();
            foreach (var aggregateId in uniqueAggregateIds)
            {
                IEnumerable<IDomainEvent> domainEvents = await _eventStore.GetDomainEventsByIdAsync(aggregateId);

                if (domainEvents.Any())
                {
                    Product product = new Product(domainEvents);
                    products.Add(product);
                }
            }
            return products;
        }

        public async Task<string?> GetSnapshotJsonAsync(Guid projectId)
        {
            string? snapshotJson = await _eventStore.GetSnapshotJsonAsync(projectId);
            return snapshotJson;
        }

        public async Task<bool> PurgeAsync()
        {
            bool success = await _eventStore.PurgeAsync();
            return success;
        }

        public async Task<bool> DeleteProductByIdAsync(Guid aggregateId)
        {
            bool success = await _eventStore.DeleteProductByIdAsync(aggregateId);
            return success;
        }
    }
}
