using Products.Read.API.DTOs;

namespace Products.Read.API.QueryResponses
{
    public class GetProductSummariesResult
    {
        public bool IsSuccess { get; set; }
        public IEnumerable<ProductSummaryDTO>? ProductSummaries { get; set; }
        public string? ErrorMessage { get; set; }

        public GetProductSummariesResult(
            bool isSuccess,
            IEnumerable<ProductSummaryDTO>? productSummaries,
            string? errorMessage)
        {
            IsSuccess = isSuccess;
            ProductSummaries = productSummaries;
            ErrorMessage = errorMessage;
        }
    }
}
