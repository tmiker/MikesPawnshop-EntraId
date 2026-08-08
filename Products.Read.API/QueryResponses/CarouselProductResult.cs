using Products.Read.API.DTOs;

namespace Products.Read.API.QueryResponses
{
    public class CarouselProductResult
    {
        public bool IsSuccess { get; set; }
        public IEnumerable<CarouselProductDTO>? CarouselProducts { get; set; }
        public string? ErrorMessage { get; set; }

        public CarouselProductResult(
            bool isSuccess,
            IEnumerable<CarouselProductDTO>? carouselProducts,
            string? errorMessage)
        {
            IsSuccess = isSuccess;
            CarouselProducts = carouselProducts;
            ErrorMessage = errorMessage;
        }
    }
}
