using Accounts.API.Abstractions;
using Accounts.API.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Accounts.API.Controllers
{
    [Route("api/accounts/[controller]")]
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
                //// Get the access token from the Authorization header
                string authHeaderPrefix = "Bearer ";
                string authorizationHeaderValue = Request.Headers.Authorization.ToString();
                string accessTokenFromHeader = string.Empty;
                if (!string.IsNullOrEmpty(authorizationHeaderValue)) accessTokenFromHeader = authorizationHeaderValue.Substring(authHeaderPrefix.Length);

                //// Create ApiUserInfoDTO and populate it with data from the access token 
                ApiUserInfoDTO apiUserInfoDTO = _tokenDecoder.GetTokenData(accessTokenFromHeader);
                _logger.LogInformation("ACCESS TOKEN FROM HEADER: {token}", accessTokenFromHeader);

                //// Log HttpContext IdToken and AccessToken
                string? identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
                // string jsonIdentityToken = JsonSerializer.Serialize(identityToken, _jsonOptions);
                _logger.LogInformation("External Accounts API method GetApiUserInfo IDENTITY TOKEN from HttpContext: {idtoken}", identityToken);
                string? accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
                // string jsonAccessToken = JsonSerializer.Serialize(accessToken, _jsonOptions);
                _logger.LogInformation("External Accounts API method GetApiUserInfo ACCESS TOKEN from HttpContext: {accesstoken}", accessToken);

                //// Add HttpContext.User.Claims to ApiUserInfoDTO and Log HttpContext.User.Claims
                List<Claim>? userClaims = HttpContext.User?.Claims.ToList();
                if (userClaims is not null && userClaims.Any())
                {
                    foreach (var claim in userClaims)
                    {
                        if (claim.Type == "roles") apiUserInfoDTO.ApiUserClaimsRolesList.Add(claim.Value);
                        apiUserInfoDTO.ApiUserClaimsClaimsList.Add($"{claim.Type} : {claim.Value}");
                    }
                    _logger.LogInformation("External Accounts API method GetApiUserInfo HTTPCONTEXT USER CLAIMS COUNT: {count}", userClaims.Count());    // 20
                }
                else
                {
                    apiUserInfoDTO.ApiUserClaimsClaimsList.Add("User.Claims did not contain any claims.");
                }

                //// Log User.Claims and User.Identity.Name and "sub" claim value
                var actionClaims = User.Claims;
                _logger.LogInformation("External Accounts API method GetApiUserInfo ACTION CLAIMS COUNT: {count}", actionClaims.Count());          // 20
                var username = User.Identity?.Name; // Works if "sub" or "name" claim is mapped
                _logger.LogInformation("External Accounts API method GetApiUserInfo was called. USERNAME: {username}", username);                  // null
                string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
                _logger.LogInformation("External Accounts API method GetApiUserInfo Owner Id: {id}.", ownerId);

                await LogIdentityInformation();

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
