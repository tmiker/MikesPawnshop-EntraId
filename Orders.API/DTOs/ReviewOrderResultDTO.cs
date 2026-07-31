namespace Orders.API.DTOs
{
    public class ReviewOrderResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
