
namespace Products.Write.Application.DTOs
{
    public class DeleteDocumentDTO
    {
        public string ProductId { get; init; } = default!;
        public string FileName { get; init; } = default!;
        public string? CorrelationId { get; set; }
    }
}
