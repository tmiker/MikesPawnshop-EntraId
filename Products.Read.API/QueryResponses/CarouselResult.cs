using Products.Read.API.DTOs;

namespace Products.Read.API.QueryResponses
{
    public class CarouselResult
    {
        public bool IsSuccess { get; set; }
        public IEnumerable<CarouselProductDTO>? CarouselProducts { get; set; }
        public string? ErrorMessage { get; set; }

        public CarouselResult(
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
