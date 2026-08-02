namespace Admin.Blazor.Client.DTOs.Products.Test
{
    public class SnapshotRecordDTO
    {
        public int Id { get; set; }
        public Guid AggregateId { get; set; }
        public string? SnapshotType { get; set; }
        public int AggregateVersion { get; set; }
        public string? SnapshotJson { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
