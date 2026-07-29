using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Products.Write.API.Auth;
using Products.Write.API.Configuration;
using Products.Write.Application.Abstractions;
using Products.Write.Application.CQRS.DevTests;
using Products.Write.Application.CQRS.QueryResults;
using Products.Write.Application.DTOs;
using Products.Write.Application.Validators;
using Products.Write.Domain.Snapshots;
using System.Security.Claims;

namespace Products.Write.API.Controllers
{
    [Route("dev/productsManagement")]
    [ApiController]
    public class DevTestsController : ControllerBase
    {
        private readonly IDevQueryService _devQueryService;
        private readonly ISender _sender;
        private readonly ITokenDecoder _tokenDecoder;
        private readonly IOptions<CloudAMQPSettings> _cloudAmqpSettings;
        private readonly ILogger<DevTestsController> _logger;

        public DevTestsController(IDevQueryService devQueryService, ISender sender, ITokenDecoder tokenDecoder, 
            IOptions<CloudAMQPSettings> cloudAmqpSettings, ILogger<DevTestsController> logger)
        {
            _devQueryService = devQueryService;
            _sender = sender;
            _tokenDecoder = tokenDecoder;
            _cloudAmqpSettings = cloudAmqpSettings;
            _logger = logger;
        }

        [HttpGet("eventCount")]
        [AllowAnonymous]
        public async Task<ActionResult> GetProductEventCount()
        {
            var result = await _devQueryService.GetEventCountAsync();
            if (result.IsSuccess) return Ok(result.EventCount);
            return BadRequest(result.ErrorMessage);
        }

        // Claims
        [HttpGet("[action]")]
        [Authorize]
        public async Task<ActionResult<ApiUserInfoDTO>> GetApiUserInfo()
        {
            try
            {
                // Get the access token from the Authorization header to parse it's claims
                string authHeaderPrefix = "Bearer ";
                string authorizationHeaderValue = Request.Headers.Authorization.ToString();
                string accessTokenFromHeader = string.Empty;
                if (!string.IsNullOrEmpty(authorizationHeaderValue)) accessTokenFromHeader = authorizationHeaderValue.Substring(authHeaderPrefix.Length);

                // Create ApiUserInfoDTO with AccessTokenClaims property populated by ITokenDecoder.GetTokenData(token) method 
                ApiUserInfoDTO apiUserInfoDTO = _tokenDecoder.GetTokenData(accessTokenFromHeader);

                // Add HttpContext.User.Claims to ApiUserInfoDTO and Log HttpContext.User.Claims
                List<Claim>? userClaims = HttpContext.User?.Claims.ToList();
                if (userClaims is not null && userClaims.Any())
                {
                    foreach (var claim in userClaims)
                    {
                        apiUserInfoDTO.ClaimsPrincipalClaims.Add(new ClaimDTO(claim));
                    }
                }

                return Ok(apiUserInfoDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GetApiUserInfo.");
                return new ApiUserInfoDTO() { ErrorMessage = $"Exception getting API Claims: {ex.Message}." };
            }
        }

        // Query service propagated endpoints

        [HttpGet("pagedAndFilteredProductSnapshots")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<PagedProductSnapshotResult>> GetPagedAndFilteredProductSnapshots(
            string? aggregateId,
            string? category,
            string? sortColumn,
            int pageNumber = 1,
            int pageSize = 10)
        {
            Console.WriteLine($"WRITE API CONTROLLER URI: {Request.GetDisplayUrl()}");
            Guid? guid = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(aggregateId)) guid = Guid.Parse(aggregateId);
            var result = await _devQueryService.GetPagedAndFilteredProductSnapshotsAsync(guid, category, sortColumn, pageNumber, pageSize);
            if (result.IsSuccess) return Ok(new PagedProductSnapshotResult() { ProductSnapshots = result.ProductSnapshots, PagingData = result.PagingData });
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("productSnapshots")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<PagedProductSnapshotResult>> GetProductSnapshots(
            string? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            Guid? guid = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(aggregateId)) guid = Guid.Parse(aggregateId);
            var result = await _devQueryService.GetProductSnapshotsAsync(guid, minVersion, maxVersion, pageNumber, pageSize);
            if (result.IsSuccess) return Ok(new PagedProductSnapshotResult() { ProductSnapshots = result.ProductSnapshots, PagingData = result.PagingData });
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("productSnapshot/{aggregateId}")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<ProductSnapshot>> GetProductSnapshot(
            string? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue)
        {
            Guid guid = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(aggregateId)) guid = Guid.Parse(aggregateId);
            var result = await _devQueryService.GetProductSnapshotByIdAsync(guid, minVersion, maxVersion);
            if (result.IsSuccess) return Ok(result.ProductSnapshot);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("eventRecords")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<PagedEventRecordResult>>  GetEventRecords(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            Guid? guid = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(aggregateId)) guid = Guid.Parse(aggregateId);
            var result = await _devQueryService.GetEventRecordsAsync(guid, correlationId, minVersion, maxVersion, pageNumber, pageSize);
            if (result.IsSuccess)
            {
                _logger.LogInformation("The Dev Tests Query Service returned {count} EventRecords.", result.EventRecords?.Count());
                return Ok(new PagedEventRecordResult() { EventRecords = result.EventRecords, PagingData = result.PagingData });
            }
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("outboxRecords")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<PagedOutboxRecordResult>> GetOutboxRecords(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            Guid? guid = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(aggregateId)) guid = Guid.Parse(aggregateId);
            var result = await _devQueryService.GetOutboxRecordsAsync(guid, correlationId, minVersion, maxVersion, pageNumber, pageSize);
            if (result.IsSuccess) return Ok(new PagedOutboxRecordResult() { OutboxRecords = result.OutboxRecords, PagingData = result.PagingData });
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("snapshotRecords")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<PagedSnapshotRecordResult>> GetSnapshotRecords(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            Guid? guid = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(aggregateId)) guid = Guid.Parse(aggregateId);
            var result = await _devQueryService.GetSnapshotRecordsAsync(guid, correlationId, minVersion, maxVersion, pageNumber, pageSize);
            if (result.IsSuccess) return Ok(new PagedSnapshotRecordResult() { SnapshotRecords = result.SnapshotRecords, PagingData = result.PagingData });
            return BadRequest(result.ErrorMessage);
        }

        // Command propagated endpoints

        [HttpPost("throwExceptionForTesting")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> ThrowExceptionForTesting([FromBody] ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            // Note passing Correlation ID from the request headers to the command as Microsoft recommends
            // caution using IHttpContextAccessor to get http context if want to pull header in handlers
            // (https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.ihttpcontextaccessor?view=aspnetcore-9.0).

            var validator = new ThrowExceptionDtoValidator();
            validator.ValidateAndThrow(throwExceptionDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            ThrowException command = new ThrowException(throwExceptionDTO, correlationId);
            ThrowExceptionResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }


        [HttpGet("getCloudAmqpSettingsTestingDummyValue")]
        [Authorize(Policy = "IsAdmin")]
        public IActionResult GetCloudAmqpTestingDummyValue(CancellationToken cancellationToken)
        {
            string? value = _cloudAmqpSettings.Value.TestingDummyValue;
            if (!string.IsNullOrWhiteSpace(value)) return Ok(value);
            return BadRequest("Unable to find the CloudAMQPSettings TestingDummyValue from CloudAMQPSettings.");
        }

        [HttpPost("purgeData")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> PurgeData([FromBody] PurgeDataDTO purgeDataDTO, CancellationToken cancellationToken)
        {
            var validator = new PurgeDataDtoValidator();
            validator.ValidateAndThrow(purgeDataDTO);

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            PurgeData command = new PurgeData(purgeDataDTO.PinNumber, correlationId);
            PurgeDataResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("permanentlyDeleteProduct")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> DeleteProductById(Guid aggregateId, CancellationToken cancellationToken)
        {

            var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];
            DeleteProduct command = new DeleteProduct(aggregateId, correlationId);
            DeleteProductResult result = await _sender.Send(command, cancellationToken);
            if (result.IsSuccess) return Ok();
            return BadRequest(result.ErrorMessage);
        }
    }
}
