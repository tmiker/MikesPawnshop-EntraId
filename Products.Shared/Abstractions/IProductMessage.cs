namespace Products.Shared.Abstractions
{
    public interface IProductMessage
    {
        Guid AggregateId { get;  }
        int AggregateVersion { get; init; }
        string? CorrelationId { get; init; } 
    }
}
