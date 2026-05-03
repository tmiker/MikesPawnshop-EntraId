using Accounts.API.Abstractions;
using Accounts.API.DTOs;
using Accounts.API.Filters;
using Accounts.API.Services;
using Accounts.API.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InternalAccountsController : ControllerBase
    {
        private readonly IInternalAccountService _internalAccountService;
        private readonly IKeyContainerService _keyContainerService;
        private readonly ILogger<InternalAccountsController> _logger;
        private readonly IConfiguration _config;

        public InternalAccountsController(
            IInternalAccountService internalAccountService,
            IKeyContainerService keyContainerService,
            ILogger<InternalAccountsController> logger,
            IConfiguration config)
        {
            _internalAccountService = internalAccountService;
            _keyContainerService = keyContainerService;
            _logger = logger;
            _config = config;
        }

        [HttpGet("[action]")]
        [OrdersApiKey]
        public IActionResult GetPublicKeyForSpecifiedContainer()
        {
            var keyContainerResult = _keyContainerService.GetPublicKeyForSpecifiedContainerAsync();
            if (keyContainerResult.IsSuccess) return Ok(keyContainerResult.KeyContainerResponse);
            else return BadRequest(keyContainerResult.ErrorMessage);
        }

        [HttpGet("status")]
        [OrdersApiKey]
        public async Task<ActionResult<AccountStatusResponseDTO?>> GetAccountStatus(AccountStatusRequestDTO requestDTO)
        {
            // the AccountStatusRequestDTO contains the KeyContainerName and the EncryptedOwnerId
            _logger.LogInformation("{this}: GetAccountDetail(AccountStatusRequestDTO requestDTO) endpoint entered", this.GetType().Name);

            // DEV ONLY: Check API Key from header via Auth Filter Attribute
            string? apiKeyFromSecrets = _config.GetValue<string>(StaticData.OrdersToAccountsApiKeyName);
            string? apiKeyFromHeader = Request.Headers[StaticData.OrdersToAccountsApiKeyHeaderName].ToString();
            if (apiKeyFromSecrets != apiKeyFromHeader)
                _logger.LogInformation("{this}:  API-Key Validation Failure - Value from header does not match Value from secrets.", this.GetType().Name);
            else
                _logger.LogInformation("{this}:  API-Key Validation Success: Value from header matches Value from secrets.", this.GetType().Name);

            AccountStatusResponseDTO result = await _internalAccountService.GetAccountStatus(requestDTO);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }

    }
}
