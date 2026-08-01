namespace Products.Read.API.DTOs
{
    public class DocumentDataDTO
    {
        public int Id { get; init; }
        public string? Name { get; init; }
        public string? Title { get; init; }
        public int SequenceNumber { get; init; }
        public string? DocumentUrl { get; init; }
        public int ProductId { get; init; }
    }
}
