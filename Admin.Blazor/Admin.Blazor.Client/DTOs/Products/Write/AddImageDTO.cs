using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace Admin.Blazor.Client.DTOs.Products.Write
{
    public class AddImageDTO
    {
        [Required]
        public string ProductId { get; set; } = default!;
        [Required]
        public string Name { get; set; } = default!;
        [Required]
        public string Caption { get; set; } = default!;
        [Required]
        public IBrowserFile? ImageBlob { get; set; }
        [Required]
        public string? BlobFileName { get; set; }
    }
}
