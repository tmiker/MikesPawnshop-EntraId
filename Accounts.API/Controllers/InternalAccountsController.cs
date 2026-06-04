using Accounts.API.Abstractions;
using Accounts.API.DTOs;
using Accounts.API.Filters;
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
            AccountStatusResponseDTO result = await _internalAccountService.GetAccountStatus(requestDTO);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }
    }
}
