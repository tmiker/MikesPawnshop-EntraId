namespace Admin.Blazor.Client.DTOs.Products.Write
{
    public class DeleteProductDTO
    {
        public string AggregateId { get; set; } = default!;
        public string? Name { get; set; }
    }
}
