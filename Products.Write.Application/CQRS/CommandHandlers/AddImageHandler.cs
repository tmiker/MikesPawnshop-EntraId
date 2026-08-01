using Azure;
using MediatR;
using Microsoft.Extensions.Logging;
using Products.Write.Application.Abstractions;
using Products.Write.Application.CQRS.CommandResults;
using Products.Write.Application.CQRS.Commands;
using Products.Write.Domain.Aggregates;
using Products.Write.Infrastructure.Abstractions;

namespace Products.Write.Application.CQRS.CommandHandlers
{
    public class AddImageHandler : IRequestHandler<AddImage, AddImageResult>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAzureStorageService _azureStorageService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<AddImageHandler> _logger;

        public AddImageHandler(IProductRepository productRepository, IAzureStorageService azureStorageService, IEventAggregator eventAggregator, ILogger<AddImageHandler> logger)
        {
            _productRepository = productRepository;
            _azureStorageService = azureStorageService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public async Task<AddImageResult> Handle(AddImage command, CancellationToken cancellationToken)
        {
            if (command.CorrelationId is null) command.CorrelationId = Guid.NewGuid().ToString();

            //validate command properties
            AddImageResult? validationResult = ValidateAddImageCommand(command);
            if (validationResult is not null) return validationResult;

            // get file name without extension so can convert all file extensions to lower case
            // set file name to user provided desired file name if provided
            var fileNameShort = Path.GetFileNameWithoutExtension(command.ImageBlob?.FileName);
            var extension = Path.GetExtension(command.ImageBlob!.FileName).ToLowerInvariant();
            string filename = string.IsNullOrWhiteSpace(command.BlobFileName) ? $"{fileNameShort}{extension}" : $"{command.BlobFileName}{extension}";

            // get product from id and upload image data for the product
            Product? product = await _productRepository.GetProductByIdAsync(command.ProductId!);
            if (product is null) return new AddImageResult(false, $"No product was found with ProductId {command.ProductId}");
            if (product.ImageFileNameExists(filename)) return new AddImageResult(false, "The selected file name already exists.");
            // int maxSequenceNumber = product.MaxImageSequenceNumber;  // moved to domain

            string containerName = $"product-{command.ProductId}";

            try
            {
                (string? ImageUrl, string? ThumbUrl) uploadResult = await _azureStorageService.UploadImageToAzureAsync(command.ImageBlob!, containerName, filename, cancellationToken);   // throws a RequestFailedException if fails
                if (uploadResult.ImageUrl is null) return new AddImageResult(false, "An image url was not returned while trying to upload the image. Please contact support.");

                product.AddImage(filename, command.Caption, uploadResult.ImageUrl!, uploadResult.ThumbUrl!, command.CorrelationId);
                bool success = await _productRepository.SaveAsync(product);
                // Note, if have success, plus fact that event store will throw if error occurs, we can confidently assume success and publish product domain events
                if (success)
                {
                    if (product.DomainEvents is not null && product.DomainEvents.Any())
                    {
                        foreach (var domainEvent in product.DomainEvents)
                        {
                            // publish the event to the bus
                            _eventAggregator.Raise(domainEvent);
                        }
                    }

                    return new AddImageResult(true, null);
                }
                else return new AddImageResult(false, "An error occurred in persisting changes.");
            }
            catch (RequestFailedException ex)
            {
                return new AddImageResult(false, ex.Message);
            }
        }

        private AddImageResult? ValidateAddImageCommand(AddImage command)
        {
            // validate command args
            command.CleanFileName();

            if (command.ImageBlob is null)
            {
                _logger.LogInformation("Attempt to add Product Image failed due to not providing an image file. CorrelationId {corrId}.", command.CorrelationId);
                return new AddImageResult(false, "An Image was not provided.");
            }
            // default file max size in bytes = 512000 bytes.
            if (command.ImageBlob.Length > 512000)
            {
                _logger.LogInformation("Attempt to add Image failed due excessive file size of {fileSize}. CorrelationId {corrId}.", command.ImageBlob.Length, command.CorrelationId);
                return new AddImageResult(false, $"The file size of {command.ImageBlob.Length} exceeds the maximum allowable size of 512,000 bytes");
            }
            // validate extension is allowed file type
            string[] permittedExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
            var extension = Path.GetExtension(command.ImageBlob.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
            {
                _logger.LogInformation("Attempt to add Product Image failed due invalid image type of {imagetype}. CorrelationId {corrId}.", extension, command.CorrelationId);
                return new AddImageResult(false, "Images must be in png, jpg, jpeg, or bmp format.");
            }
            return null;
        }
    }
}
