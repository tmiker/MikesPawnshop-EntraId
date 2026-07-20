using Products.Shared.Messages;

namespace Products.Read.API.Abstractions
{
    public interface IProductRepository
    {
        Task AddProductAsync(ProductAddedMessage message);
        Task UpdateProductStatusAsync(StatusUpdatedMessage message);
        Task AddProductImageAsync(ImageAddedMessage message);
        Task AddProductDocumentAsync(DocumentAddedMessage message);
        Task<bool> PurgeAsync();
        Task<bool> DeleteProductByAggregateIdAsync(Guid aggregateId);
    }
}
