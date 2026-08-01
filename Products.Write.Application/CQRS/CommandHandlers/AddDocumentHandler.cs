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
    public class AddDocumentHandler : IRequestHandler<AddDocument, AddDocumentResult>
    {
        private readonly IProductRepository _productRepository;
        private readonly IAzureStorageService _azureStorageService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<AddDocumentHandler> _logger;

        public AddDocumentHandler(IProductRepository productRepository, IAzureStorageService azureStorageService, IEventAggregator eventAggregator, ILogger<AddDocumentHandler> logger)
        {
            _productRepository = productRepository;
            _azureStorageService = azureStorageService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public async Task<AddDocumentResult> Handle(AddDocument command, CancellationToken cancellationToken)
        {
            if (command.CorrelationId is null) command.CorrelationId = Guid.NewGuid().ToString();

            //validate command properties
            AddDocumentResult? validationResult = ValidateAddDocumentCommand(command);
            if (validationResult is not null) return validationResult;

            // get file name without extension so can convert all file extensions to lower case
            // set file name to user provided desired file name if provided
            var fileNameShort = Path.GetFileNameWithoutExtension(command.DocumentBlob?.FileName);
            var extension = Path.GetExtension(command.DocumentBlob!.FileName).ToLowerInvariant();
            string filename = string.IsNullOrWhiteSpace(command.BlobFileName) ? $"{fileNameShort}{extension}" : $"{command.BlobFileName}{extension}";

            // get product from id and upload image data for the product
            Product? product = await _productRepository.GetProductByIdAsync(command.ProductId!);
            if (product is null) return new AddDocumentResult(false, $"No product was found with ProductId {command.ProductId}");
            if (product.DocumentFileNameExists(filename)) return new AddDocumentResult(false, "The selected file name already exists.");
            // int maxSequenceNumber = product.MaxDocumentSequenceNumber;   // moved to domain

            string containerName = $"product-{command.ProductId}";

            try
            {
                string? docUrl = await _azureStorageService.UploadDocumentToAzureAsync(command.DocumentBlob!, containerName, filename, cancellationToken);   // throws a RequestFailedException if fails
                if (docUrl is null) return new AddDocumentResult(false, "A document url was not returned while trying to upload the document. Please contact support.");

                product.AddDocument(filename, command.Title, docUrl, command.CorrelationId);
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

                    return new AddDocumentResult(true, null);
                }
                else return new AddDocumentResult(false, "An error occurred in persisting changes.");
            }
            catch (RequestFailedException ex)
            {
                return new AddDocumentResult(false, ex.Message);
            }
        }

        private AddDocumentResult? ValidateAddDocumentCommand(AddDocument command)
        {
            // validate command args
            command.CleanFileName();

            if (command.DocumentBlob is null)
            {
                _logger.LogInformation("Attempt to add Product Document failed due to not providing a document file. CorrelationId {corrId}.", command.CorrelationId);
                return new AddDocumentResult(false, "A Document was not provided.");
            }
            // default file max size in bytes = 512000 bytes.
            if (command.DocumentBlob.Length > 512000)
            {
                _logger.LogInformation("Attempt to add Document failed due excessive file size of {fileSize}. CorrelationId {corrId}.", command.DocumentBlob.Length, command.CorrelationId);
                return new AddDocumentResult(false, $"The file size of {command.DocumentBlob.Length} exceeds the maximum allowable size of 512,000 bytes");
            }
            // validate extension is allowed file type
            string[] permittedExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".txt" };
            var extension = Path.GetExtension(command.DocumentBlob.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
            {
                _logger.LogInformation("Attempt to add Product Document failed due invalid document type of {doctype}. CorrelationId {corrId}.", extension, command.CorrelationId);
                return new AddDocumentResult(false, "Images must be in doc, docx, xls, xlsx, pdf or txt format.");
            }
            return null;
        }
    }
}
