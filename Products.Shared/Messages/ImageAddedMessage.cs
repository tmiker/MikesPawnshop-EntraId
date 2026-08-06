using Products.Shared.Abstractions;

namespace Products.Shared.Messages
{
    public class ImageAddedMessage : IProductMessage
    {
        // for logging purposes on read side
        public Guid AggregateId { get; init; }
        public string AggregateType { get; init; } = default!;
        public int AggregateVersion { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CorrelationId { get; init; } 
        // command data
        public string? Name { get; init; } 
        public string? Caption { get; init; } 
        public int SequenceNumber { get; init; }
        public string? ImageUrl { get; init; } 
        public string? ThumbnailUrl { get; init; } 

        public ImageAddedMessage(Guid aggregateId, string aggregateType, int aggregateVersion,
            string? correlationId, string name, string caption, int sequenceNumber,
            string imageUrl, string thumbnailUrl)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = correlationId;
            Name = name;
            Caption = caption;
            SequenceNumber = sequenceNumber;
            ImageUrl = imageUrl;
            ThumbnailUrl = thumbnailUrl;
        }
    }
}
