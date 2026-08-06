using Products.Read.API.Domain.Models;
using Products.Read.API.QueryResponses;

namespace Products.Read.API.Abstractions
{
    public interface IProductQueryService
    {
        IAsyncEnumerable<Product> GetProductsAsAsyncEnumerable();

        Task<GetProductsResult> GetAllProductsAsync();

        Task<GetPagedAndFilteredProductsResult> GetPagedAndFilteredProductsAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10);

        Task<GetProductByIdResult> GetProductByIdAsync(int id);

        Task<GetProductSummariesResult> GetAllProductSummariesAsync();

        Task<GetPagedAndFilteredProductSummariesResult> GetPagedAndFilteredProductSummariesAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10);

        Task<GetProductSummaryByIdResult> GetProductSummaryByIdAsync(int id);

        Task<CarouselResult> GetCarouselProductImagesAsync();

        Task<(bool IsSuccess, int ProductCount, string? ErrorMessage)> GetProductCountAsync();
    }
}
