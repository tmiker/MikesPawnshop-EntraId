using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Products.Write.Application.Abstractions;
using Products.Write.Application.Configuration;

namespace Products.Write.Application.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly IImageResizeHelper _imageResizeHelper;
        private readonly ILogger<AzureStorageService> _logger;
        private readonly IOptions<AzureSettings> _azureSettings;

        public AzureStorageService(IConfiguration configuration, IImageResizeHelper imageResizeHelper, ILogger<AzureStorageService> logger, IOptions<AzureSettings> azureSettings)
        {
            _configuration = configuration;
            _imageResizeHelper = imageResizeHelper;
            _logger = logger;
            _azureSettings = azureSettings;
        }

        public async Task<(string? ImageUrl, string? ThumbUrl)> UploadImageToAzureAsync(IFormFile file, string containerName, string desiredFileName, CancellationToken cancellationToken)
        {
            string? azureConnectionString = _configuration["AZURE_BLOB_STORAGE_CONNECTION"] ?? throw new ArgumentNullException("Azure Connection String");

            // Note: BlobServiceClient.CreateBlobContainerAsync THROWS if the container exists
            // Note: Don't assign reture of BlobContainerInfo result to BlobContainerClient.CreateIfNotExistsAsync as it throws if already exists (returns null)

            BlobContainerClient? containerClient = new BlobContainerClient(azureConnectionString, containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
            if (containerClient is null) throw new ArgumentNullException($"Error creating blob storage container. Container Name: {containerName}");

            string imgDirFileName = $"imgs/{desiredFileName}";
            BlobClient blobClient = containerClient.GetBlobClient(imgDirFileName);      // can set blob filename here - file is the file referenced below as 'file'

            using var imageStream = new MemoryStream();
            await file.CopyToAsync(imageStream);
            imageStream.Position = 0;
            await blobClient.UploadAsync(imageStream, cancellationToken);

            ////// once blob is created can set metadata
            ////IDictionary<string, string> metadata = new Dictionary<string, string>() { { "SequenceNumber", sequenceNumber.ToString() } };
            ////imageBlobClient.SetMetadata(metadata);

            string imageUrl = blobClient.Uri.AbsoluteUri;
            string? thumbUrl = null;

            // use thumbnail helper to create thumbnail, then upload to azure in virtual directory 'thumbnails/filename.ext'
            byte[] original = imageStream.ToArray();
            byte[]? thumbnail = _imageResizeHelper.ResizeImage(original, 200);

            if (thumbnail != null)
            {
                string thumbDirFileName = $"thumbs/{desiredFileName}";
                BlobClient thumbBlobClient = containerClient.GetBlobClient(thumbDirFileName);

                using var thumbStream = new MemoryStream();
                thumbStream.Write(thumbnail, 0, thumbnail.Length);
                thumbStream.Position = 0;
                await thumbBlobClient.UploadAsync(thumbStream, cancellationToken);
                thumbUrl = thumbBlobClient.Uri.AbsoluteUri;
            }
            else
            {
                _logger.LogError("Magick.NET Error creating thumbnail for image in container {container}, image file name {filename}", containerName, desiredFileName);
            }

            return (imageUrl, thumbUrl);

        }

        public async Task<string> UploadDocumentToAzureAsync(IFormFile file, string containerName, string desiredFileName, CancellationToken cancellationToken)
        {
            string? azureConnectionString = _configuration["AZURE_BLOB_STORAGE_CONNECTION"] ?? throw new ArgumentNullException("Azure Connection String");

            // Note: BlobServiceClient.CreateBlobContainerAsync THROWS if the container exists
            // Note: Don't assign reture of BlobContainerInfo result to BlobContainerClient.CreateIfNotExistsAsync as it throws if already exists (returns null)

            BlobContainerClient? containerClient = new BlobContainerClient(azureConnectionString, containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);
            if (containerClient is null) throw new ArgumentNullException($"Error creating blob storage container. Container Name: {containerName}");

            string docDirFileName = $"docs/{desiredFileName}";
            BlobClient blobClient = containerClient.GetBlobClient(docDirFileName);      // can set blob filename here - file is reference below as 'file'

            using var docStream = new MemoryStream();
            await file.CopyToAsync(docStream);
            docStream.Position = 0;
            await blobClient.UploadAsync(docStream, cancellationToken);
            string docUrl = blobClient.Uri.AbsoluteUri;

            return docUrl;
        }

        public async Task<BlobContainerClient?> CreateContainerAsync(string azureConnectionString, string containerName, CancellationToken cancellationToken)
        {
            /// IMPORTANT:  The container name may only contain lowercase letters, numbers, and hyphens, and must begin with a letter or a number. 
            /// Each hyphen must be preceded and followed by a non-hyphen character. The name must also be between 3 and 63 characters long.

            BlobServiceClient serviceClient = new BlobServiceClient(azureConnectionString);
            // to allow anonymous read access to the blob set PublicAccessType.Blob below, to allow only private access set PublicAccessType.None
            PublicAccessType accessType = PublicAccessType.BlobContainer;
            BlobContainerClient containerClient = await serviceClient.CreateBlobContainerAsync(containerName, accessType, null, cancellationToken);    //, PublicAccessType.BlobContainer, null, cancellationToken);
            if (await containerClient.ExistsAsync()) return containerClient;
            _logger.LogWarning("Error creating a storage container with metadata using BlobServiceClient. PublicAccessType: {accessType}.", accessType.ToString());
            return containerClient;
        }

        public async Task<BlobContainerClient?> CreateContainerWithMetadataAsync(string azureConnectionString, string containerName, CancellationToken cancellationToken)
        {
            BlobServiceClient serviceClient = new BlobServiceClient(azureConnectionString);
            PublicAccessType accessType = PublicAccessType.BlobContainer;
            Dictionary<string, string> metadata = new Dictionary<string, string>() { { "container name", containerName } };
            // to allow anonymous read access to the blob set PublicAccessType.Blob below, to allow only private access set PublicAccessType.None
            BlobContainerClient containerClient = await serviceClient.CreateBlobContainerAsync(containerName, accessType, metadata, cancellationToken);    //, PublicAccessType.BlobContainer, null, cancellationToken);
            if (await containerClient.ExistsAsync()) return containerClient;
            _logger.LogWarning("Error creating a storage container with metadata using BlobServiceClient. PublicAccessType: {accessType}.", accessType.ToString());
            return containerClient;
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductImageFromAzureAsync(string containerName, string fileName, CancellationToken cancellationToken)
        {
            string imageFileName = $"imgs/{fileName}";
            string thumbFileName = $"thumbs/{fileName}";

            string? azureConnectionString = _configuration["AZURE_BLOB_STORAGE_CONNECTION"] ?? throw new ArgumentNullException("Azure Connection String");

            BlobContainerClient blobContainerClient = new BlobContainerClient(azureConnectionString, containerName);
            bool imgSuccess = await blobContainerClient.DeleteBlobIfExistsAsync(imageFileName, DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken);
            bool thumbSuccess = await blobContainerClient.DeleteBlobIfExistsAsync(thumbFileName, DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken);

            if (imgSuccess && thumbSuccess) return (true, null);
            else throw new RequestFailedException($"Unable to remove all images and thumbnails from Azure Storage Container Name: {containerName}, File Name: {fileName}");
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductDocumentFromAzureAsync(string containerName, string fileName, CancellationToken cancellationToken)
        {
            string docFileName = $"docs/{fileName}";
            string? azureConnectionString = _configuration["AZURE_BLOB_STORAGE_CONNECTION"] ?? throw new ArgumentNullException("Azure Connection String");

            BlobContainerClient blobContainerClient = new BlobContainerClient(azureConnectionString, containerName);
            bool success = await blobContainerClient.DeleteBlobIfExistsAsync(docFileName, DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken);

            if (success) return (true, null);
            else throw new RequestFailedException($"Unable to remove documents from Azure Storage Container Name: {containerName}, File Name: {fileName}");
        }
    }
}
