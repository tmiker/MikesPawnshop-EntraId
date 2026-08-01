using Products.Write.Domain.Base;

namespace Products.Write.Domain.Events
{
    public class StatusUpdated : IDomainEvent
    {
        public Guid AggregateId { get; init; }
        public string AggregateType { get; init; } = default!;
        public int AggregateVersion { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CorrelationId { get; init; } 
        public string Status { get; init; } = default!;

        public StatusUpdated(Guid aggregateId, string aggregateType, int aggregateVersion,
            string? correlationId, string status)   
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = correlationId;
            Status = status;
        }
    }
}
