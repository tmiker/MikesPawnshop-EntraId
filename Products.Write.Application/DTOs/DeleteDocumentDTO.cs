using System.ComponentModel.DataAnnotations;

namespace Products.Write.Application.DTOs
{
    public class DeleteDocumentDTO
    {
        public string ProductId { get; init; } = default!;
        public string FileName { get; init; } = default!;
        public string? CorrelationId { get; set; }


        //public DeleteDocumentDTO(string productId, string fileName, string? title, string? correlationId)
        //{
        //    ProductId = productId;
        //    FileName = fileName;
        //    Title = title;
        //    CorrelationId = correlationId;
        //}
    }
}
