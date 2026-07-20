using Products.Shared.Abstractions;

namespace Products.Shared.Messages
{
    public class ProductDeletedMessage : IProductMessage
    {
        // for logging purposes on read side
        public Guid AggregateId { get; init; }
        public string? AggregateType { get; init; } = default!;
        public int AggregateVersion { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CorrelationId { get; init; }

        public ProductDeletedMessage(Guid aggregateId, string? aggregateType, int aggregateVersion, string? correlationId)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = correlationId;
        }
    }
}
