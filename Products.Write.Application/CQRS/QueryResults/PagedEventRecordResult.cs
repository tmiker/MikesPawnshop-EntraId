using Products.Write.Application.Paging;
using Products.Write.Infrastructure.Data;

namespace Products.Write.Application.CQRS.QueryResults
{
    public class PagedEventRecordResult
    {
        public IEnumerable<EventRecord>? EventRecords { get; set; }
        public PaginationMetadata? PagingData { get; set; }
    }
}
