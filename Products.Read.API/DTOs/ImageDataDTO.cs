namespace Products.Read.API.DTOs
{
    public class ImageDataDTO
    {
        public int Id { get; init; }
        public string? Name { get; init; }           // virtual directory plus filename
        public string? Caption { get; init; }
        public int SequenceNumber { get; init; }
        public string? ImageUrl { get; init; }
        public string? ThumbnailUrl { get; init; }
        public int ProductId { get; init; }
    }
}
