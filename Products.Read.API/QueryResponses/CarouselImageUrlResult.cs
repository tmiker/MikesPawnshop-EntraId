using Products.Read.API.DTOs;

namespace Products.Read.API.QueryResponses
{
    public class CarouselImageUrlResult
    {
        public bool IsSuccess { get; set; }
        public IEnumerable<string>? CarouselImages { get; set; }
        public string? ErrorMessage { get; set; }

        public CarouselImageUrlResult(
            bool isSuccess,
            IEnumerable<string>? carouselImages,
            string? errorMessage)
        {
            IsSuccess = isSuccess;
            CarouselImages = carouselImages;
            ErrorMessage = errorMessage;
        }
    }
}
