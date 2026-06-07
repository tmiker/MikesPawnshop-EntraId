using Products.Write.Application.Paging;
using Products.Write.Domain.Snapshots;
using Products.Write.Infrastructure.Data;

namespace Products.Write.Application.Abstractions
{
    public interface IDevQueryService
    {
        Task<(bool IsSuccess, IEnumerable<ProductSnapshot>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedAndFilteredProductSnapshotsAsync(
            Guid? aggregateId,
            string? category,
            string? sortColumn,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, IEnumerable<ProductSnapshot>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetProductSnapshotsAsync(
            Guid? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, ProductSnapshot? ProductSnapshot, string? ErrorMessage)> GetProductSnapshotByIdAsync(
            Guid aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue);

        Task<(bool IsSuccess, IEnumerable<EventRecord>? EventRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetEventRecordsAsync(
            Guid? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, IEnumerable<OutboxRecord>? OutboxRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetOutboxRecordsAsync(
            Guid? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, IEnumerable<SnapshotRecord>? SnapshotRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetSnapshotRecordsAsync(
            Guid? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, int EventCount, string? ErrorMessage)> GetEventCountAsync();
    }
}
