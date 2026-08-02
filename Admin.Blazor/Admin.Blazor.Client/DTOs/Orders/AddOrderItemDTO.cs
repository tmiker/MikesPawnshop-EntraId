using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Admin.Blazor.Client.DTOs.Orders
{
    public class AddOrderItemDTO
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public string? AggregateId { get; set; }
        public string? Category { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Currency { get; set; }
        public decimal Price { get; set; }
        [Required]
        public string? UOM { get; set; }
        public double Quantity { get; set; }

        [JsonIgnore]
        public decimal LineTotalPrice
        {
            get
            {
                return Price * (decimal)Quantity;
            }
        }
    }
}
