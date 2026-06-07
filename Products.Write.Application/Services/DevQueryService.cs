using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Products.Write.Application.Abstractions;
using Products.Write.Application.Paging;
using Products.Write.Domain.Aggregates;
using Products.Write.Domain.Base;
using Products.Write.Domain.Snapshots;
using Products.Write.Infrastructure.Data;
using Products.Write.Infrastructure.DataAccess;

namespace Products.Write.Application.Services
{
    public class DevQueryService : IDevQueryService
    {
        private readonly EventStoreDbContext _db;
        private readonly ILogger<DevQueryService> _logger;

        public DevQueryService(EventStoreDbContext db, ILogger<DevQueryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductSnapshot>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedAndFilteredProductSnapshotsAsync(
            Guid? aggregateId,
            string? category,
            string? sortColumn,
            int pageNumber = 1,
            int pageSize = 10)
        {
            List<ProductSnapshot>? snapshots = [];

            List<Guid> uniqueIds = new List<Guid>();
            if (aggregateId is null || aggregateId == default(Guid)) uniqueIds.AddRange(await GetUniqueAggregateIdsAsync());
            else uniqueIds.Add(aggregateId.Value);

            foreach (var id in uniqueIds)
            {
                IEnumerable<IDomainEvent> aggregateEvents = await GetDomainEventsByAggregateIdAndVersionAsync(id, 0, int.MaxValue);
                Product product = new Product(aggregateEvents);
                snapshots.Add(product.GetSnapshot());
            }

            var query = snapshots.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(s => s.Category != null && s.Category.ToLower().Contains(category.ToLower()));

            switch (sortColumn?.ToLower())
            {
                case "id":
                    query = query.OrderBy(p => p.Id);
                    break;
                case "name":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "category":
                    query = query.OrderBy(p => p.Category);
                    break;
                case "price ascending":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price descending":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            IEnumerable<ProductSnapshot> records = query.ToList();
            int totalCount = records.Count();

            IEnumerable<ProductSnapshot> result = records.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            
            PaginationMetadata pagingData = new PaginationMetadata(totalCount, pageSize, pageNumber);

            return (true, result, pagingData, null);
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductSnapshot>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetProductSnapshotsAsync(
            Guid? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            List<ProductSnapshot>? snapshots = [];

            List<Guid> uniqueIds = new List<Guid>();
            if (aggregateId is null || aggregateId == default(Guid)) uniqueIds.AddRange(await GetUniqueAggregateIdsAsync());
            else uniqueIds.Add(aggregateId.Value);

            foreach (var id in uniqueIds)
            {
                IEnumerable<IDomainEvent> aggregateEvents = await GetDomainEventsByAggregateIdAndVersionAsync(id, minVersion, maxVersion);
                Product product = new Product(aggregateEvents);
                snapshots.Add(product.GetSnapshot());
            }

            int totalCount = snapshots.Count();

            IEnumerable<ProductSnapshot> result = snapshots.OrderBy(p => p.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            
            PaginationMetadata pagingData = new PaginationMetadata(totalCount, pageSize, pageNumber);

            return (true, result, pagingData, null);
        }

        public async Task<(bool IsSuccess, ProductSnapshot? ProductSnapshot, string? ErrorMessage)> GetProductSnapshotByIdAsync(
            Guid aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue)
        {
            IEnumerable<IDomainEvent> aggregateEvents = await GetDomainEventsByAggregateIdAndVersionAsync(aggregateId, minVersion, maxVersion);
            Product product = new Product(aggregateEvents);
            ProductSnapshot snapshot = product.GetSnapshot();
            return (true, snapshot, null);
        }

        private async Task<IEnumerable<Guid>> GetUniqueAggregateIdsAsync()
        {
            IEnumerable<Guid> uniqueIds = await _db.EventRecords.Select(p => p.AggregateId).Distinct().ToListAsync();        
            return uniqueIds;
        }
        private async Task<IEnumerable<IDomainEvent>> GetDomainEventsByAggregateIdAndVersionAsync(Guid aggregateId, int minVersion, int maxVersion)
        {
            IEnumerable<EventRecord> records = await _db.EventRecords.Where(p => p.AggregateId == aggregateId
                && p.AggregateVersion >= minVersion && p.AggregateVersion <= maxVersion).ToListAsync();

            List<IDomainEvent> domainEvents = new List<IDomainEvent>();
            foreach (EventRecord record in records)
            {
                var @event = JsonConvert.DeserializeObject(record.EventJson, Type.GetType(record.EventType)!)!;
                domainEvents.Add((IDomainEvent)@event!);
            }

            return domainEvents;
        }

        public async Task<(bool IsSuccess, IEnumerable<EventRecord>? EventRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetEventRecordsAsync(
            Guid? aggregateId, 
            string? correlationId = null, 
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1, 
            int pageSize = 10)
        {
            IQueryable<EventRecord> query = _db.EventRecords.AsQueryable();
            if (aggregateId is not null && aggregateId != default(Guid)) query = query.Where(e => e.AggregateId == aggregateId);
            if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(e => e.CorrelationId == correlationId);
            query = query.Where(e => e.AggregateVersion >= minVersion && e.AggregateVersion <= maxVersion);

            List<EventRecord> records = await query.ToListAsync();
            int totalCount = records.Count();

            var result = records.OrderBy(e => e.OccurredAt).Skip((pageNumber - 1) * pageSize).Take(pageSize);

            PaginationMetadata pagingData = new PaginationMetadata(totalCount, pageSize, pageNumber);

            return (true, result, pagingData, null);
        }

        public async Task<(bool IsSuccess, IEnumerable<OutboxRecord>? OutboxRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetOutboxRecordsAsync(
            Guid? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            IQueryable<OutboxRecord> query = _db.OutboxRecords.AsQueryable();
            if (aggregateId is not null && aggregateId != default(Guid)) query = query.Where(o => o.AggregateId == aggregateId);
            if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(o => o.CorrelationId == correlationId);
            query = query.Where(o => o.AggregateVersion >= minVersion && o.AggregateVersion <= maxVersion);
            query = query.OrderBy(o => o.OccurredAt);

            List<OutboxRecord> records = await query.ToListAsync();
            int totalCount = records.Count();

            List<OutboxRecord> result = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            
            PaginationMetadata pagingData = new PaginationMetadata(totalCount, pageSize, pageNumber);

            return (true, result, pagingData, null);
        }

        public async Task<(bool IsSuccess, IEnumerable<SnapshotRecord>? SnapshotRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetSnapshotRecordsAsync(
            Guid? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            IQueryable<SnapshotRecord> query = _db.SnapshotRecords.AsQueryable();
            if (aggregateId is not null && aggregateId != default(Guid)) query = query.Where(r => r.AggregateId == aggregateId);
            query = query.Where(r => r.AggregateVersion >= minVersion && r.AggregateVersion <= maxVersion);

            List<SnapshotRecord> records = await query.ToListAsync();
            int totalCount = records.Count();

            List<SnapshotRecord> result = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            PaginationMetadata pagingData = new PaginationMetadata(totalCount, pageSize, pageNumber);

            return (true, result, pagingData, null);
        }

        public async Task<(bool IsSuccess, int EventCount, string? ErrorMessage)> GetEventCountAsync()
        {
            try
            {
                int count = await _db.EventRecords.CountAsync();
                return (true, count, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting event records");
                return (false, 0, "An error occurred connecting with the Products Write-Side Database. Please try again later.");
            }
        }
    }
}
