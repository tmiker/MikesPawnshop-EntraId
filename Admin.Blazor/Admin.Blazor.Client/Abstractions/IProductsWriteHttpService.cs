using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Products.Test;
using Admin.Blazor.Client.DTOs.Products.Write;
using Admin.Blazor.Client.Paging;

namespace Admin.Blazor.Client.Abstractions
{
    public interface IProductsWriteHttpService
    {
        Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync();
        Task<(bool IsSuccess, int EventCount, string? ErrorMessage)> GetProductEventRecordCountAsync();
        Task<(bool IsSuccess, Guid? AggregateId, string? ErrorMessage)> AddProductAsync(AddProductDTO addProductDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> UpdateStatusAsync(UpdateStatusDTO updateStatusDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> AddProductImageAsync(AddImageDTO addImageDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> AddProductDocumentAsync(AddDocumentDTO addDocumentDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductImageAsync(DeleteImageDTO deleteImageDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductDocumentAsync(DeleteDocumentDTO deleteDocumentDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> CheckAzureStorageStatusAsync();

        // Dev Tests
        Task<(bool IsSuccess, IEnumerable<ProductSnapshotDTO>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedAndFilteredProductSnapshotsAsync(
            string? aggregateId,
            string? category,
            string? sortColumn,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, IEnumerable<ProductSnapshotDTO>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedProductSnapshotsAsync(
            string? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, ProductSnapshotDTO? ProductSnapshot, string? ErrorMessage)> GetProductSnapshotByIdAsync(
            string? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue);

        Task<(bool IsSuccess, IEnumerable<EventRecordDTO>? EventRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedEventRecordsAsync(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, IEnumerable<OutboxRecordDTO>? OutboxRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedOutboxRecordsAsync(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, IEnumerable<SnapshotRecordDTO>? SnapshotRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedSnapshotRecordsAsync(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10);

        Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> PurgeDataAsync(PurgeDataDTO purgeDataDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductByIdAsync(Guid aggregateId, CancellationToken cancellationToken);
    }
}
