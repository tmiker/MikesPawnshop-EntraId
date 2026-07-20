using Products.Write.Domain.Aggregates;

namespace Products.Write.Infrastructure.Abstractions
{
    public interface IProductRepository
    {
        Task<bool> SaveAsync(Product product);
        Task<Product> GetProductByIdAsync(Guid aggregateId);
        Task<Product> GetProductByIdAndVersionAsync(Guid aggregateId, int minVersion, int maxVersion);

        // SNAPSHOTS
        Task<Product?> GetProductByIdUsingSnapshotsAsync(Guid aggregateId);

        // DEV / ADMIN
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<string?> GetSnapshotJsonAsync(Guid projectId);
        Task<bool> PurgeAsync();
        Task<bool> DeleteProductByIdAsync(Guid aggregateId);
    }
}
