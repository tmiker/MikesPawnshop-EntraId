using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace Admin.Blazor.Client.DTOs.Products.Write
{
    public class AddDocumentDTO
    {
        [Required]
        public string ProductId { get; set; } = default!;
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public string Title { get; set; } = default!;
        [Required]
        public IBrowserFile? DocumentBlob { get; set; }
        [Required]
        public string? BlobFileName { get; set; }
    }
}
