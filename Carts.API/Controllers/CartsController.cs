using Carts.API.Abstractions;
using Carts.API.DTOs;
using Carts.API.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text;
using System.Text.Json;

namespace Carts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ITokenDecoder _tokenDecoder;
        private readonly ILogger<CartsController> _logger;

        JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() { WriteIndented = true };

        public CartsController(ICartService cartService, ITokenDecoder tokenDecoder, ILogger<CartsController> logger)
        {
            _cartService = cartService;
            _tokenDecoder = tokenDecoder;
            _logger = logger;
        }

        [HttpPost("items")]
        // [Authorize]
        public async Task<ActionResult<int>> AddNewCartItem(AddShoppingCartItemDTO addShoppingCartItemDTO)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null)
            {
                // throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
                // return Unauthorized($"User identity information unavailable. Unauthorized access to restricted resource.");
                ownerId = "3"; // TEMPORARY WORKAROUND FOR TESTING PURPOSES ONLY
            }
            var result = await _cartService.AddNewCartItemAsync(ownerId, addShoppingCartItemDTO);
            if (result.IsSuccess) return Ok(result.CartItemQuantity);
            else return BadRequest("Error adding the item to your cart. Please contact support.");
        }

        [HttpPut("items")]
        // [Authorize]
        public async Task<IActionResult> UpdateProductQuantity(string aggregateId, int amount)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null)
            {
                // throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
                return Unauthorized($"User identity information unavailable. Unauthorized access to restricted resource.");
                // ownerId = "3"; // TEMPORARY WORKAROUND FOR TESTING PURPOSES ONLY
            }
            bool success = await _cartService.UpdateCartItemQuantityAsync(ownerId, aggregateId, amount);
            if (success) return NoContent();
            else return BadRequest("Error updating product quantity in shopping cart. Ensure a cart containing the corresponding product exists.");
        }

        [HttpDelete("items")]
        // [Authorize]
        public async Task<IActionResult> RemoveCartItem(string aggregateId)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null)
            {
                // throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
                return Unauthorized($"User identity information unavailable. Unauthorized access to restricted resource.");
                // ownerId = "3"; // TEMPORARY WORKAROUND FOR TESTING PURPOSES ONLY
            }
            bool success = await _cartService.RemoveCartItemAsync(ownerId, aggregateId);
            if (success) return NoContent();
            else return BadRequest("Error removing the item from your cart. Please contact support.");
        }

        [HttpGet]
        // [Authorize]
        public async Task<ActionResult<ShoppingCartDTO?>> GetShoppingCart()
        {
            await LogIdentityInformation();
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null)
            {
                // throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
                return Unauthorized($"User identity information unavailable. Unauthorized access to restricted resource.");
                // ownerId = "3"; // TEMPORARY WORKAROUND FOR TESTING PURPOSES ONLY
            }
            //return BadRequest($"User identity information unavailable. Unauthorized access to restricted resource.");
            ShoppingCartDTO? cartDTO = await _cartService.GetCartAsync(ownerId);
            return Ok(cartDTO);
        }

        [HttpDelete]
        // [Authorize]
        public async Task<IActionResult> RemoveShoppingCart()
        {
            //await LogIdentityInformation();
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null)
            {
                // throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
                return Unauthorized($"User identity information unavailable. Unauthorized access to restricted resource.");
                // ownerId = "3"; // TEMPORARY WORKAROUND FOR TESTING PURPOSES ONLY
            }
            //return BadRequest($"User identity information unavailable. Unauthorized access to restricted resource.");
            bool success = await _cartService.RemoveCartAsync(ownerId);
            if (success) return NoContent();
            else return BadRequest("Error removing shopping cart.");
        }

        private async Task LogIdentityInformation()
        {
            // get saved identity token
            var identityToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            var userClaimsStringBuilder = new StringBuilder();
            foreach (var claim in User.Claims)
            {
                userClaimsStringBuilder.AppendLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
            }
            _logger.LogInformation($"LOG INDENTITY INFORMATION METHOD RESULT: ");
            _logger.LogInformation($"Identity Token: \n{identityToken}\n   ");
            _logger.LogInformation($"Access Token: \n{accessToken}\n");
            _logger.LogInformation($"User Claims: \n{userClaimsStringBuilder}\n");
        }

        private string? GetCurrentOwnerId()
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");
            return ownerId;
        }
    }
}
