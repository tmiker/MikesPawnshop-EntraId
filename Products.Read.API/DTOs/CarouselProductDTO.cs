namespace Products.Read.API.DTOs
{
    public class CarouselProductDTO
    {
        public string? Name { get; init; }
        public string? Category { get; init; }
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public string? Currency { get; init; }
        public int QuantityOnHand { get; init; }
        public string? UOM { get; init; }
        public ImageDataDTO? ImageData { get; init; }
    }
}
