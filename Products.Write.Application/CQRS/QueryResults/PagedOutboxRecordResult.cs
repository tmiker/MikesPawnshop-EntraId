using Products.Write.Application.Paging;
using Products.Write.Infrastructure.Data;

namespace Products.Write.Application.CQRS.QueryResults
{
    public class PagedOutboxRecordResult
    {
        public IEnumerable<OutboxRecord>? OutboxRecords { get; set; }
        public PaginationMetadata? PagingData { get; set; }
    }
}
