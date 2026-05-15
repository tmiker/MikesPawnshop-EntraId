using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Products.Write.Application.Abstractions;
using Products.Write.Application.Configuration;
using Products.Write.Application.CQRS.CommandResults;
using Products.Write.Application.CQRS.Commands;
using Products.Write.Application.CQRS.DevTests;
using Products.Write.Application.DTOs;
using Products.Write.Application.Validators;

namespace Products.Write.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsManagementController : ControllerBase
    {
        private readonly ISender _sender;
        // private readonly IOptions<AzureSettings> _azureSettings;
        private readonly ILogger<ProductsManagementController> _logger;
        private readonly IConfiguration _config;

        public ProductsManagementController(ISender sender, ILogger<ProductsManagementController> logger, IConfiguration config)
        {
            _sender = sender;
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<ActionResult<AddProductResult>> AddProduct([FromBody] AddProductDTO addProductDTO, CancellationToken cancellationToken)
        {
            // Note passing Correlation ID from the request headers to the command as Microsoft recommends
            // caution using IHttpContextAccessor to get http context if want to pull header in handlers
            // (https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.ihttpcontextaccessor?view=aspnetcore-9.0).

            var validator = new AddProductDtoValidator();
            validator.ValidateAndThrow(addProductDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            AddProduct command = new AddProduct(addProductDTO, correlationId);

            AddProductResult result = await _sender.Send(command, cancellationToken);  

            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("image")]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<ActionResult<AddImageResult>> AddImage([FromForm] AddImageDTO addImageDTO, CancellationToken cancellationToken)
        {
            var validator = new AddImageDtoValidator();
            validator.ValidateAndThrow(addImageDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            AddImage command = new AddImage(addImageDTO, correlationId);
            AddImageResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("document")]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<ActionResult<AddDocumentResult>> AddDocument([FromForm] AddDocumentDTO addDocumentDTO, CancellationToken cancellationToken)
        {
            var validator = new AddDocumentDtoValidator();
            validator.ValidateAndThrow(addDocumentDTO);

            if (addDocumentDTO.DocumentBlob is null) Console.WriteLine($"The Document Blob is null.");
            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            AddDocument command = new AddDocument(addDocumentDTO, correlationId);
            AddDocumentResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("status")]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<ActionResult<UpdateStatusResult>> UpdateStatus([FromBody] UpdateStatusDTO updateStatusDTO, CancellationToken cancellationToken)
        {
            var validator = new UpdateStatusDtoValidator();
            validator.ValidateAndThrow(updateStatusDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            UpdateStatus command = new UpdateStatus(updateStatusDTO, correlationId);
            UpdateStatusResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("image")]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<ActionResult<DeleteImageResult>> DeleteImage(DeleteImageDTO deleteImageDTO, CancellationToken cancellationToken)
        {
            var validator = new DeleteImageDtoValidator();
            validator.ValidateAndThrow(deleteImageDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            DeleteImage command = new DeleteImage(deleteImageDTO, correlationId);
            DeleteImageResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }
        [HttpDelete("document")]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<ActionResult<DeleteDocumentResult>> DeleteDocument(DeleteDocumentDTO deleteDocumentDTO, CancellationToken cancellationToken)
        {
            var validator = new DeleteDocumentDtoValidator();
            validator.ValidateAndThrow(deleteDocumentDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            DeleteDocument command = new DeleteDocument(deleteDocumentDTO, correlationId);
            DeleteDocumentResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("[action]")]
        // [Authorize(Policy = "IsAdminOrManager")]
        public async Task<IActionResult> AzureBlobStoragePingTest()
        {
            string? pingTestUri = _config.GetValue<string>("AZURE_BLOB_STORAGE_PING_TEST_URI");
            if (string.IsNullOrWhiteSpace(pingTestUri)) return BadRequest("The Azure Storage ping test URI could not be found.");
            AzurePingTest pingTest = new AzurePingTest(pingTestUri);
            AzurePingTestResult pingResult = await _sender.Send(pingTest);
            if (pingResult.IsSuccess) return Ok("Ping Success!");
            return BadRequest("Azure Storage not available. Verify network firewall settings allow current IP address.");
        }
    }
}
