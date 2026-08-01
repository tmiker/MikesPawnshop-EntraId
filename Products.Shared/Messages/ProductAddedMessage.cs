using Products.Shared.Abstractions;

namespace Products.Shared.Messages
{
    public class ProductAddedMessage : IProductMessage
    {
        // for logging purposes on read side
        public Guid AggregateId { get; init; }
        public string AggregateType { get; init; } = default!;
        public int AggregateVersion { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CorrelationId { get; init; } 
        // command data
        public string Name { get; init; } 
        public string Category { get; init; } 
        public string Description { get; init; } 
        public decimal Price { get; init; }
        public string Currency { get; init; } 
        public string Status { get; init; } 
        public int QuantityOnHand { get; init; }
        public int QuantityAllocated { get; init; }
        public string UOM { get; init; }
        public int LowStockThreshold { get; init; }

        public ProductAddedMessage(Guid aggregateId, string aggregateType, int aggregateVersion,
            string? correlationId, string name, string category, string description, decimal price, 
            string currency, string status, int quantityOnHand, int quantityAllocated, string uom, int lowStockThreshold)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            OccurredAt = DateTime.UtcNow;
            CorrelationId = correlationId;
            Name = name;
            Category = category;
            Description = description;
            Price = price;
            Currency = currency;
            Status = status;
            QuantityOnHand = quantityOnHand;
            QuantityAllocated = quantityAllocated;
            UOM = uom;
            LowStockThreshold = lowStockThreshold;
        }
    }
}
