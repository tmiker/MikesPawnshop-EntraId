using Products.Write.Domain.Base;
using Products.Write.Domain.Snapshots;
using Products.Write.Infrastructure.Data;

namespace Products.Write.Infrastructure.Abstractions
{
    public interface IProductEventStore
    {
        Task<bool> SaveEventRecordsAsync(IEnumerable<IDomainEvent> events);
        Task<IEnumerable<IDomainEvent>> GetDomainEventsByIdAsync(Guid aggregateId, int minVersion = 0, int maxVersion = Int32.MaxValue);

        // SNAPSHOT RECORDS
        Task<bool> SaveAsSnapshotRecordAsync(ProductSnapshot snapshot);
        Task<ProductSnapshot?> GetProductSnapshotAsync(Guid aggregateId);

        // OUTBOX RECORDS
        Task<IEnumerable<OutboxRecord>> GetOutboxRecordsAsync();

        // DEV ONLY
        Task<IEnumerable<Guid>> GetUniqueAggregateIdsAsync();
        Task<string?> GetSnapshotJsonAsync(Guid aggregateId);
        Task<bool> RemoveAllProductEventRecordsByIdAsync(Guid aggregateId);
        Task<bool> PurgeAllProductEventRecordsAsync();
        Task<bool> PurgeAsync();
        Task<bool> DeleteProductByIdAsync(Guid aggregateId);
    }
}
