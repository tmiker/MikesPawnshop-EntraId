using Consumer.Blazor.Client.DTOs.Products;
using Consumer.Blazor.Client.DTOs.Products.Test;
using Consumer.Blazor.Client.Paging;

namespace Consumer.Blazor.Client.Abstractions
{
    public interface IProductsReadHttpService
    {
        // IAsyncEnumerable<ProductDTO> StreamProductsAsync();
        Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, string? ErrorMessage)> GetProductsAsync();
        Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? ProductSummaries, string? ErrorMessage)> GetProductSummariesAsync();
        Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductsAsync(string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10);
        Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductSummariesAsync(string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10);
        Task<(bool IsSuccess, ProductDTO? Product, string? ErrorMessage)> GetProductByIdAsync(int id);
        Task<(bool IsSuccess, ProductSummaryDTO? ProductSummary, string? ErrorMessage)> GetProductSummaryByIdAsync(int id);
        Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken);
    }
}
