using Products.Shared.Abstractions;

namespace Products.Shared.Messages
{
    public class DocumentDeletedMessage : IProductMessage
    {
        // for logging purposes on read side
        public Guid AggregateId { get; init; }
        public string AggregateType { get; init; } = default!;
        public int AggregateVersion { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CorrelationId { get; init; } 
        // command data
        public string FileName { get; init; } = default!;

        public DocumentDeletedMessage(Guid aggregateId, string aggregateType, int aggregateVersion,
            string? correlationId, string fileName)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = correlationId;
            FileName = fileName;
        }
    }
}
