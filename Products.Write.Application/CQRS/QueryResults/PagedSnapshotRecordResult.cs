using Products.Write.Application.Paging;
using Products.Write.Infrastructure.Data;

namespace Products.Write.Application.CQRS.QueryResults
{
    public class PagedSnapshotRecordResult
    {
        public IEnumerable<SnapshotRecord>? SnapshotRecords { get; set; }
        public PaginationMetadata? PagingData { get; set; }
    }
}
