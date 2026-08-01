using Products.Shared.Abstractions;

namespace Products.Shared.Messages
{
    public class DataPurgedMessage : IProductMessage
    {
        public Guid AggregateId { get; } = Guid.Empty;
        public int AggregateVersion { get; init; } = -1;
        public string? CorrelationId { get; init; } = "0";
    }
}
