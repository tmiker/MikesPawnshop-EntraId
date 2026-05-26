using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Products.Read.API.Abstractions;
using Products.Read.API.Auth;
using Products.Read.API.Configuration;
using Products.Read.API.DTOs.DevTests;
using Products.Read.API.Exceptions;
using Products.Read.Validators;
using System.Security.Claims;

namespace Products.Read.API.Controllers
{
    [Route("dev/products")]
    [ApiController]
    public class DevTestsController : ControllerBase
    {
        private readonly IOptions<CloudAMQPSettings> _cloudAmqpSettings;
        private readonly ITokenDecoder _tokenDecoder;
        private readonly ILogger<DevTestsController> _logger;

        public DevTestsController(IOptions<CloudAMQPSettings> cloudAmqpSettings, ITokenDecoder tokenDecoder, ILogger<DevTestsController> logger)
        {
            _cloudAmqpSettings = cloudAmqpSettings;
            _tokenDecoder = tokenDecoder;
            _logger = logger;
        }

        // Claims
        [HttpGet("[action]")]
        [Authorize]
        public async Task<ActionResult<ApiUserInfoDTO>> GetApiUserInfo()
        {
            var contextClaims = HttpContext.User.Claims;
            _logger.LogInformation("Products Read API method GetApiUserInfo HTTPCONTEXT CLAIMS COUNT: {count}", contextClaims.Count());    // 20
            var actionClaims = User.Claims;
            _logger.LogInformation("Products Read API method GetApiUserInfo ACTION CLAIMS COUNT: {count}", actionClaims.Count());          // 20
            var username = User.Identity?.Name; // Works if "sub" or "name" claim is mapped
            _logger.LogInformation("Products Read API method GetApiUserInfo was called. USERNAME: {username}", username);                  // null
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            _logger.LogInformation("Products Read API method GetApiUserInfo Owner Id: {id}.", ownerId);                                    

            string authHeaderPrefix = "Bearer ";
            string authorizationHeaderValue = Request.Headers.Authorization.ToString();
            string accessTokenFromHeader = authorizationHeaderValue.Substring(authHeaderPrefix.Length);

            string? identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
            // string jsonIdentityToken = JsonSerializer.Serialize(identityToken, _jsonOptions);
            _logger.LogInformation("Products Read API method GetApiUserInfo IDENTITY TOKEN from HttpContext: {idtoken}", identityToken);
            string? accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            // string jsonAccessToken = JsonSerializer.Serialize(accessToken, _jsonOptions);
            _logger.LogInformation("Products Read API method GetApiUserInfo ACCESS TOKEN from HttpContext: {accesstoken}", accessToken);

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

        [HttpPost("throwExceptionForTesting")]
        [Authorize]
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

        [HttpGet("getCloudAmqpSettingsTestingDummyValue")]
        [Authorize]
        public IActionResult GetCloudAmqpTestingDummyValue(CancellationToken cancellationToken)
        {
            string? value = _cloudAmqpSettings.Value.TestingDummyValue;
            if (!string.IsNullOrWhiteSpace(value)) return Ok(value);
            return BadRequest("Unable to find the CloudAMQPSettings TestingDummyValue.");
        }
    }
}
