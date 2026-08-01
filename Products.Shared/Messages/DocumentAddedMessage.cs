using Products.Shared.Abstractions;

namespace Products.Shared.Messages
{
    public class DocumentAddedMessage : IProductMessage
    {
        // for logging purposes on read side
        public Guid AggregateId { get; init; }
        public string AggregateType { get; init; } = default!;
        public int AggregateVersion { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CorrelationId { get; init; } 
        // command data
        public string? Name { get; init; } 
        public string? Title { get; init; } 
        public int SequenceNumber { get; init; }
        public string? DocumentUrl { get; init; } 

        public DocumentAddedMessage(Guid aggregateId, string aggregateType, int aggregateVersion,
            string? correlationId, string name, string title, int sequenceNumber,
            string documentUrl)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = correlationId;
            Name = name;
            Title = title;
            SequenceNumber = sequenceNumber;
            DocumentUrl = documentUrl;
        }
    }
}
