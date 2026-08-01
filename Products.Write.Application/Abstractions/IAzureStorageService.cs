using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;

namespace Products.Write.Application.Abstractions
{
    public interface IAzureStorageService
    {
        Task<(string? ImageUrl, string? ThumbUrl)> UploadImageToAzureAsync(IFormFile file, string containerName, string desiredFileName, CancellationToken cancellationToken);
        Task<string> UploadDocumentToAzureAsync(IFormFile file, string containerName, string desiredFileName, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductImageFromAzureAsync(string containerName, string fileName, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductDocumentFromAzureAsync(string containerName, string fileName, CancellationToken cancellationToken);

        // BELOW ONLY FOR USE WHEN ADDING A PRODUCT IF DESIRED AS THROWS IF CONTAINER ALREADY EXISTS
        Task<BlobContainerClient?> CreateContainerAsync(string azureConnectionString, string containerName, CancellationToken cancellationToken);
        Task<BlobContainerClient?> CreateContainerWithMetadataAsync(string azureConnectionString, string containerName, CancellationToken cancellationToken);
    }
}
