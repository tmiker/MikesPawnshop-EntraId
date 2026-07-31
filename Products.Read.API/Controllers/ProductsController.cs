using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Products.Read.API.Abstractions;
using Products.Read.API.Configuration;
using Products.Read.API.Domain.Models;
using Products.Read.API.DTOs;
using Products.Read.API.DTOs.DevTests;
using Products.Read.API.Exceptions;
using Products.Read.API.QueryResponses;
using Products.Read.Validators;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.OutputCaching;

namespace Products.Read.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductQueryService _productQueryService;
        private readonly IOptions<CloudAMQPSettings> _cloudAmqpSettings;
        private readonly ITokenDecoder _tokenDecoder;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductQueryService productQueryService, IOptions<CloudAMQPSettings> cloudAmqpSettings, ITokenDecoder tokenDecoder, ILogger<ProductsController> logger)
        {
            _productQueryService = productQueryService;
            _cloudAmqpSettings = cloudAmqpSettings;
            _tokenDecoder = tokenDecoder;
            _logger = logger;
        }

        [HttpGet("productCount")]
        [AllowAnonymous]
        public async Task<ActionResult> GetProductCount()
        {
            var result = await _productQueryService.GetProductCountAsync();
            if (result.IsSuccess) return Ok(result.ProductCount);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("productStream")]
        [AllowAnonymous]
        public async IAsyncEnumerable<Product> StreamProducts()
        {
            await foreach (var product in _productQueryService.GetProductsAsAsyncEnumerable())
            {
                yield return product;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts()
        {
            GetProductsResult result = await _productQueryService.GetAllProductsAsync();
            if (result.IsSuccess) return Ok(result.Products);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("summaries")]
        [AllowAnonymous]
        [OutputCache(PolicyName = "StorefrontProductsCachePolicy")]
        public async Task<ActionResult<IEnumerable<ProductSummaryDTO>>> GetProductSummaries()
        {
            GetProductSummariesResult result = await _productQueryService.GetAllProductSummariesAsync();
            if (result.IsSuccess) return Ok(result.ProductSummaries);
            return BadRequest(result.ErrorMessage);
        }

        // ResponseCache Location:
        //   Any - both the client and server will be able to cache the response, which is equivalent to the public directive of the cache-control header
        //   Client - changes the cache-control header value to private which means that only the client can cache the response
        //   None - sets both the cache-control and pragma header to no-cache, which means the client cannot use a cached response without revalidating with the server 

        [HttpGet("paged")]
        [AllowAnonymous]
        // [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "StorefrontProductsCachePolicy")]
        public async Task<ActionResult<PagedProductsDTO>> GetPagedAndFilteredProducts(string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            GetPagedAndFilteredProductsResult result = await _productQueryService.GetPagedAndFilteredProductsAsync(filter, category, sortColumn, pageNumber, pageSize);
            if (result.IsSuccess) return Ok(new PagedProductsDTO { Products = result.Products, PagingData = result.PaginationMetadata, FetchTime = DateTime.Now });
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("paged/summaries")]
        [AllowAnonymous]
        // [ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { "*" }, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "StorefrontProductsCachePolicy")]
        public async Task<ActionResult<PagedProductSummariesDTO>> GetPagedAndFilteredProductSummaries(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            GetPagedAndFilteredProductSummariesResult result = await _productQueryService.GetPagedAndFilteredProductSummariesAsync(filter, category, sortColumn, pageNumber, pageSize);
            if (result.IsSuccess) return Ok(new PagedProductSummariesDTO { ProductSummaries = result.ProductSummaries, PagingData = result.PaginationMetadata, FetchTime = DateTime.Now });
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        // [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "id" })]
        [OutputCache(PolicyName = "StorefrontProductsCachePolicy")]
        public async Task<ActionResult<ProductDTO>> GetProductById(int id)
        {
            GetProductByIdResult result = await _productQueryService.GetProductByIdAsync(id);
            if (result.IsSuccess)
            {
                if (result.Product is not null) result.Product.FetchTime = DateTime.Now;
                return Ok(result.Product);
            }
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("summary/{id}")]
        [AllowAnonymous]
        // [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "id" })]
        [OutputCache(PolicyName = "StorefrontProductsCachePolicy")]
        public async Task<ActionResult<ProductSummaryDTO>> GetProductSummaryById(int id)
        {
            GetProductSummaryByIdResult result = await _productQueryService.GetProductSummaryByIdAsync(id);
            if (result.IsSuccess)
            {
                if (result.ProductSummary is not null) result.ProductSummary.FetchTime = DateTime.Now;
                return Ok(result.ProductSummary);
            }
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("throwExceptionForTesting")]
        [AllowAnonymous]
        public IActionResult ThrowExceptionForTesting([FromBody] ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            // Note passing Correlation ID from the request headers to the command as Microsoft recommends
            // caution using IHttpContextAccessor to get http context if want to pull header in handlers
            // (https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.ihttpcontextaccessor?view=aspnetcore-9.0).

            var validator = new ThrowExceptionDtoValidator();
            validator.ValidateAndThrow(throwExceptionDTO);

            Exception ex = throwExceptionDTO.ExceptionType.ToLower() switch
            {
                "validationexception" => throw new ValidationException(        //"This is a test FluentValidation.ValidationException thrown from ThrowExceptionHandler"),
                        "This is a test FluentValidation.ValidationException thrown from ThrowExceptionHandler and should enumerate two validation errors.",
                        new List<ValidationFailure>()
                        {
                            new ValidationFailure("Property 1", "This is a Property 1 error"),
                            new ValidationFailure("Property 2", "This is Property 2's first error"),
                            new ValidationFailure("Property 2", "This is Property 2's second error")
                        }),
                "unauthorizedaccessexception" => throw new UnauthorizedAccessException("This is a test UnauthorizedAccessException thrown from ThrowExceptionHandler."),
                "forbiddenexception" => throw new ForbiddenException("This is a test ForbiddenException thrown from ThrowExceptionHandler."),
                "notfoundexception" => throw new NotFoundException("This is a test NotFoundException thrown from ThrowExceptionHandler."),
                "conflictexception" => throw new ConflictException("This is a test ConflictException thrown from ThrowExceptionHandler."),
                "argumentexception" => throw new ArgumentException("This is a test ArgumentException thrown from ThrowExceptionHandler."),
                "argumentnullexception" => throw new ArgumentNullException("This is a test ArgumentNullException thrown from ThrowExceptionHandler."),
                "invalidoperationexception" => throw new InvalidOperationException("This is a test InvalidOperationException thrown from ThrowExceptionHandler."),
                "taskcanceledexception" => throw new TaskCanceledException("This is a test TaskCanceledException thrown from ThrowExceptionHandler."),
                _ => throw new Exception("This is a test general Exception thrown from ThrowExceptionHandler.")
            };

            return Ok();
        }
    }
}
