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
using Products.Write.Auth;
using Products.Write.Domain.Snapshots;
using System.Security.Claims;

namespace Products.Write.API.Controllers
{
    [Route("api/productsManagement/[controller]")]
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

        // Claims
        [HttpGet("[action]")]
        [Authorize]
        public async Task<ActionResult<ApiUserInfoDTO>> GetApiUserInfo()
        {
            var contextClaims = HttpContext.User.Claims;
            _logger.LogInformation("Products Write API method GetApiUserInfo HTTPCONTEXT CLAIMS COUNT: {count}", contextClaims.Count());    // 20
            var actionClaims = User.Claims;
            _logger.LogInformation("Products Write API method GetApiUserInfo ACTION CLAIMS COUNT: {count}", actionClaims.Count());          // 20
            var username = User.Identity?.Name; // Works if "sub" or "name" claim is mapped
            _logger.LogInformation("Products Write API method GetApiUserInfo was called. USERNAME: {username}", username);                  // null
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            _logger.LogInformation("Products Write API method GetApiUserInfo Owner Id: {id}.", ownerId);                                    

            string authHeaderPrefix = "Bearer ";
            string authorizationHeaderValue = Request.Headers.Authorization.ToString();
            string accessTokenFromHeader = authorizationHeaderValue.Substring(authHeaderPrefix.Length);

            string? identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
            // string jsonIdentityToken = JsonSerializer.Serialize(identityToken, _jsonOptions);
            _logger.LogInformation("Products Write API method GetApiUserInfo IDENTITY TOKEN from HttpContext: {idtoken}", identityToken);
            string? accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            // string jsonAccessToken = JsonSerializer.Serialize(accessToken, _jsonOptions);
            _logger.LogInformation("Products Write API method GetApiUserInfo ACCESS TOKEN from HttpContext: {accesstoken}", accessToken);

            ApiUserInfoDTO apiUserInfoDTO = _tokenDecoder.GetTokenData(accessTokenFromHeader);

            List<Claim> userClaims = HttpContext.User.Claims.ToList();
            if (userClaims.Any())
            {
                foreach (var claim in userClaims)
                {
                    if (claim.Type == "roles") apiUserInfoDTO.ApiUserClaimsRolesList.Add(claim.Value);
                    apiUserInfoDTO.ApiUserClaimsClaimsList.Add($"{claim.Type} : {claim.Value}");
                }
            }
            else
            {
                apiUserInfoDTO.ApiUserClaimsClaimsList.Add("User.Claims did not contain any claims.");
            }
            return Ok(apiUserInfoDTO);
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
    }
}
