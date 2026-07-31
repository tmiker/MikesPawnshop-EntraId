namespace Orders.API.DTOs
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
    }
}
