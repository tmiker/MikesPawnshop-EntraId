using Admin.Blazor.Client.DTOs.Claims;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Products;
using Admin.Blazor.Client.DTOs.Products.Test;
using Admin.Blazor.Client.Paging;

namespace Admin.Blazor.Client.Abstractions
{
    public interface IPublicProductsReadHttpService
    {
        Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync();
        IAsyncEnumerable<ProductDTO> StreamProductsAsync();
        Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, string? ErrorMessage)> GetProductsAsync();
        Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? ProductSummaries, string? ErrorMessage)> GetProductSummariesAsync();
        Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductsAsync(string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10);
        Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductSummariesAsync(string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10);
        Task<(bool IsSuccess, ProductDTO? Product, string? ErrorMessage)> GetProductByIdAsync(int id);
        Task<(bool IsSuccess, ProductSummaryDTO? ProductSummary, string? ErrorMessage)> GetProductSummaryByIdAsync(int id);

        // Dev Tests
        Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken);

        // No claims for Products Read API as it is a public service
        // Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetProductsReadApiUserInfoAsync();
    }
}
