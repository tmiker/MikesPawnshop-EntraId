using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Orders.API.Abstractions;
using Orders.API.Auth;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Orders.API.Controllers
{
    [Route("api/orders/[controller]")]
    [ApiController]
    public class DevTestsController : ControllerBase
    {
        private readonly ITokenDecoder _tokenDecoder;
        private readonly ILogger<DevTestsController> _logger;

        JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() { WriteIndented = true };

        public DevTestsController(ITokenDecoder tokenDecoder, ILogger<DevTestsController> logger)
        {
            _tokenDecoder = tokenDecoder;
            _logger = logger;
        }

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

        private async Task LogIdentityInformation()
        {
            try
            {
                // get saved identity token
                var identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
                var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
                var userClaimsStringBuilder = new StringBuilder($"LOG INDENTITY INFORMATION METHOD RESULT: \n");
                foreach (var claim in User.Claims)
                {
                    userClaimsStringBuilder.AppendLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
                }
                _logger.LogInformation("LOG INDENTITY INFORMATION METHOD RESULT: ");
                _logger.LogInformation("Identity Token: \n{identityToken}\n", identityToken);
                _logger.LogInformation("Access Token: \n{accessToken}\n", accessToken);
                _logger.LogInformation("User Claims: \n{userClaimsStringBuilder}\n", userClaimsStringBuilder);
            }
            catch (Exception ex) { _logger.LogError(ex, "ERROR: An error occurred while logging identity information: {ex.Message}", ex.Message); }
        }
    }
}
