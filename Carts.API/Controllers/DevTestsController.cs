using Carts.API.Abstractions;
using Carts.API.Auth;
using Carts.API.DTOs;
using Carts.API.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Carts.API.Controllers
{
    [Route("api/carts/[controller]")]
    [ApiController]
    public class DevTestsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ITokenDecoder _tokenDecoder;
        private readonly ILogger<DevTestsController> _logger;

        JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() { WriteIndented = true };

        public DevTestsController(ICartService cartService, ITokenDecoder tokenDecoder, ILogger<DevTestsController> logger)
        {
            _cartService = cartService;
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

        [HttpGet("[action]")]
        public async Task<ActionResult<ShoppingCartDTO?>> GetShoppingCartByOwnerId(string id)
        {
            ShoppingCartDTO? cartDTO = await _cartService.GetCartAsync(id);
            if (cartDTO is not null) return Ok(cartDTO);
            return NotFound($"A shopping cart with Owner Id {id} was not found.");
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

        private string? GetCurrentOwnerId()
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
            return ownerId;
        }

        [HttpGet("testApplicationException")]
        public IActionResult TestApplicationException()
        {
            string testResult = $"External Carts TestApplicationException Endpoint called. Throwing Application Exception.";
            if (testResult != "success") throw new CartsDomainApplicationException($"{testResult} || Carts domain application exception thrown.");
            return Ok(testResult);
        }

        [HttpGet("testException")]
        public IActionResult TestException()
        {
            string testResult = $"External Carts TestException Endpoint called. Throwing Exception.";
            if (testResult != "success") throw new CartsDomainException($"{testResult} || Carts domain exception thrown.");
            return Ok(testResult);
        }
    }
}
