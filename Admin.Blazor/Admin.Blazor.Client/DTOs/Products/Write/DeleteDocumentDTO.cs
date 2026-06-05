using System.ComponentModel.DataAnnotations;

namespace Admin.Blazor.Client.DTOs.Products.Write
{
    public class DeleteDocumentDTO
    {
        [Required]
        public string ProductId { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string? CorrelationId { get; set; }

        public DeleteDocumentDTO(string productId, string fileName, string? correlationId)
        {
            ProductId = productId;
            FileName = fileName;
            CorrelationId = correlationId;
        }
    }
}
