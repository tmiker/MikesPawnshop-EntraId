using System.Text.Json.Serialization;

namespace Consumer.Blazor.Client.DTOs.Orders
{
    public class AddOrderItemDTO
    {
        public int ProductId { get; set; }
        public string? AggregateId { get; set; }
        public string? Category { get; set; }
        public string? Name { get; set; }
        public string? Currency { get; set; }
        public decimal Price { get; set; }
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
