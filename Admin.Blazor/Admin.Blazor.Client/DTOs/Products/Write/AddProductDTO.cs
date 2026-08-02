using System.ComponentModel.DataAnnotations;

namespace Admin.Blazor.Client.DTOs.Products.Write
{
    public class AddProductDTO
    {
        [Required]
        public string? Name { get; set; } = default!;
        [Required]
        public string? Category { get; set; } = default!;
        [Required]
        public string? Description { get; set; } = default!;
        public decimal Price { get; set; }
        [Required]
        public string? Currency { get; set; } = default!;
        [Required]
        public string? Status { get; set; } = default!;
        public int QuantityOnHand { get; set; }
        [Required]
        public string? UOM { get; set; } = default!;
        public int LowStockThreshold { get; set; }
    }
}
