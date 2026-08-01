using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Products.Read.API.Abstractions;
using Products.Read.API.Auth;
using Products.Read.API.Configuration;
using System.Security.Claims;

namespace Products.Read.API.Controllers
{
    [Route("dev/products")]
    [ApiController]
    public class DevTestsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly IOptions<CloudAMQPSettings> _cloudAmqpSettings;
        private readonly ITokenDecoder _tokenDecoder;
        private readonly ILogger<DevTestsController> _logger;

        public DevTestsController(IProductRepository productRepository, IOptions<CloudAMQPSettings> cloudAmqpSettings, ITokenDecoder tokenDecoder, ILogger<DevTestsController> logger)
        {
            _productRepository = productRepository;
            _cloudAmqpSettings = cloudAmqpSettings;
            _tokenDecoder = tokenDecoder;
            _logger = logger;
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
