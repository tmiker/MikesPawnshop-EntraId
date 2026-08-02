namespace Admin.Blazor.Client.DTOs.Products.Test
{
    public class OutboxRecordDTO
    {
        public int Id { get; set; }
        public int EventRecordId { get; set; }
        public Guid AggregateId { get; set; }
        public string? AggregateType { get; set; }
        public int AggregateVersion { get; set; }
        public string? EventType { get; set; }
        public string? EventJson { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? CorrelationId { get; set; }
        public bool IsPublished { get; set; } 
    }
}
