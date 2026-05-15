using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Products.Write.Infrastructure.Data
{
    [Table("Outbox Records", Schema = "dbo")]
    public class OutboxRecord
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int EventRecordId { get; set; }
        public Guid AggregateId { get; set; }
        public string AggregateType { get; set; }
        public int AggregateVersion { get; set; }
        public string EventType { get; set; }
        public string EventJson { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? CorrelationId { get; set; }
        public bool IsPublished { get; set; } 

        public OutboxRecord(EventRecord eventRecord)
        {
            EventRecordId = eventRecord.Id;
            AggregateId = eventRecord.AggregateId;
            AggregateType = eventRecord.AggregateType;
            AggregateVersion = eventRecord.AggregateVersion;
            EventType = eventRecord.EventType;
            EventJson = eventRecord.EventJson;
            OccurredAt = eventRecord.OccurredAt;
            CorrelationId = eventRecord.CorrelationId;
            IsPublished = false;
        }

        public OutboxRecord(Guid aggregateId, string aggregateType, int aggregateVersion, string eventType, string eventJson, DateTime occurredAt, string correlationId)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            EventType = eventType;
            EventJson = eventJson;
            OccurredAt = occurredAt;
            CorrelationId = correlationId;
            IsPublished = false;
        }
    }
}
