using Accounts.API.Abstractions;
using Accounts.API.DTOs;
using Accounts.API.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Accounts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountsController> _logger;
        private readonly IAccountDataMapper _mapper;

        public AccountsController(IAccountService accountService, ILogger<AccountsController> logger, IAccountDataMapper mapper)
        {
            _accountService = accountService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("accountEstablished")]
        [Authorize]
        public async Task<ActionResult<bool>> AccountIsEstablished()
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var result = await _accountService.GetAccountByOwnerIdAsync(ownerId);

            if (result.IsSuccess) return Ok(true);
            return BadRequest(false);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<AccountDTO>> GetByOwnerId()
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var result = await _accountService.GetAccountByOwnerIdAsync(ownerId);

            //if (result.Account is not null)
            //{
            //    string jsonAccount = JsonSerializer.Serialize(result.Account);
            //    _logger.LogInformation("Account Retrieved: {@result.Account}", result.Account);
            //}

            if (result.IsSuccess) return Ok(result.Account);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("{accountId}")]
        [Authorize]
        public async Task<ActionResult<AccountDTO>> GetByAccountId(string accountId)
        {
            var result = await _accountService.GetAccountByAccountIdAsync(accountId);
            if (result.IsSuccess) return Ok(result.Account);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post(AddAccountDTO addAccountDTO)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var result = await _accountService.CreateAccountAsync(ownerId, addAccountDTO);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("addAddress")]
        [Authorize]
        public async Task<IActionResult> Put(AddAddressDTO addAddressDTO)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var result = await _accountService.AddAddressAsync(ownerId, addAddressDTO);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("[action]")]
        [Authorize(Roles = "Admin")]
        public ActionResult<string> AllowAdminUserByRole()
        {
            bool isAdmin = User.IsInRole("Admin");
            string result = $"API Controller Authorization by Roles; User.IsInRole(Admin) = {isAdmin}";
            List<Claim> userClaims = User.Claims.ToList();
            if (userClaims.Any())
            {
                foreach (var claim in userClaims)
                {
                    if (claim.Type == ClaimTypes.Role) result += $";  User has ClaimTypes.Role value of {claim.Value}";
                    if (claim.Type == "role") result += $";  User has \n'role\n' claim value of {claim.Value}";
                    // break;
                }
            }
            return Ok(result);
        }

        [HttpGet("[action]")]
        [Authorize(Policy = "IsAdmin")]
        public ActionResult<string> AllowAdminUserByPolicy()
        {
            bool isAdmin = User.IsInRole("Admin");
            string result = $"API Controller Authorization by Policy; User.IsInRole(Admin) = {isAdmin}";
            List<Claim> userClaims = User.Claims.ToList();
            if (userClaims.Any())
            {
                foreach (var claim in userClaims)
                {
                    if (claim.Type == ClaimTypes.Role) result += $";  User has ClaimTypes.Role value of {claim.Value}\n";
                    if (claim.Type == "role") result += $";  User has \n'role\n' claim value of {claim.Value}";
                    // break;
                }
            }
            return Ok(result);
        }

        [HttpPost("testCreateAccount")]
        [AllowAnonymous]
        public async Task<IActionResult> TestCreateAccount()
        {
            var accountExists = await _accountService.GetAccountByOwnerIdAsync("testOwnerId");
            if (accountExists.IsSuccess && accountExists.Account is not null) return BadRequest("Test account already exists. Delete existing test account before creating a new one.");

            AddressDTO addressDTO = new AddressDTO()
            {
                IsPrimaryBilling = true,
                IsPrimaryShipping = true,
                Street1 = "123 Main St",
                Street2 = "Apt 4B",
                City = "Anytown",
                State = "TX",
                PostalCode = "12345"
            };
            AddAccountDTO addAccountDTO = new AddAccountDTO()
            {
                FirstName = "Test",
                LastName = "User",
                Email = "testuser@somemail.com",
                PhoneNumber = "123-456-7890",
                Addresses = new List<AddressDTO>() { addressDTO }
            };
            string? ownerId = "testOwnerId";
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var result = await _accountService.CreateAccountAsync(ownerId, addAccountDTO);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("getTestAccount")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTestAccount()
        {
            string? ownerId = "testOwnerId";
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var result = await _accountService.GetAccountByOwnerIdAsync(ownerId);

            if (result.Account is not null)
            {
                string jsonAccount = JsonSerializer.Serialize(result.Account);
                _logger.LogInformation("Account Retrieved: {@result.Account}", result.Account);
            }

            if (result.IsSuccess) return Ok(result.Account);
            return BadRequest(result.ErrorMessage);
        }
    }
}
