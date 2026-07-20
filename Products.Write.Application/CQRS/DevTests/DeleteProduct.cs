using MediatR;
using Products.Write.Domain.Aggregates;
using System.ComponentModel.DataAnnotations;

namespace Products.Write.Application.CQRS.DevTests
{
    public class DeleteProduct : IRequest<DeleteProductResult>
    {
        [Required]
        public Guid AggregateId { get; init; }
        public string? CorrelationId { get; set; } = default!;

        public DeleteProduct(Guid aggregatgeId, string? correlationId)
        {
            AggregateId = aggregatgeId;
            CorrelationId = correlationId;
        }
    }
}
