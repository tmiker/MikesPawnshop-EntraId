namespace Orders.API.DTOs
{
    public class ReviewOrderResultDTO
    {
        public bool IsSuccess { get; set; }

        public string? AccountId { get; set; }
        public string? AccountStatus { get; set; }

        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
