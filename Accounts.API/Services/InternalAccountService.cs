using Accounts.API.Abstractions;
using Accounts.API.Domain.Models;
using Accounts.API.DTOs;
using MongoDB.Driver;

namespace Accounts.API.Services
{
    public class InternalAccountService : IInternalAccountService
    {
        private readonly IConfiguration _config;
        private readonly IMongoCollection<Account> _accounts;
        private readonly IAccountDataMapper _mapper;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ILogger<InternalAccountService> _logger;

        private const int _baseCreditLimit = 5000;

        public InternalAccountService(
            IConfiguration config,
            IAccountDataMapper mapper,
            IEncryptionHelper encryptionHelper,
            ILogger<InternalAccountService> logger)
        {
            _config = config;
            string? environment = _config["ASPNETCORE_ENVIRONMENT"];
            var client = environment == "Development" ? new MongoClient(_config["LOCAL_MONGO_CONNECTION"]) : new MongoClient(_config["AZURE_MONGO_CONNECTION"]);
            var database = client.GetDatabase(_config["MONGO_DATABASE"]);
            _accounts = database.GetCollection<Account>(_config["MONGO_ACCOUNT_COLLECTION"]);
            _mapper = mapper;
            _encryptionHelper = encryptionHelper;
            _logger = logger;
        }

        public async Task<AccountStatusResponseDTO> GetAccountStatus(AccountStatusRequestDTO requestDTO)
        {
            AccountStatusResponseDTO accountStatusResponse = new AccountStatusResponseDTO();

            if (string.IsNullOrWhiteSpace(requestDTO.EncryptedOwnerId))
            {
                accountStatusResponse.IsSuccess = false;
                accountStatusResponse.Errors.Add("Missing Required Request Data.");
                return accountStatusResponse;
            }

            string aesKey = _config["IntAcctsAesSymEncryption_Key"] ?? throw new InvalidOperationException("AES key is not configured.");
            string aesIV = _config["IntAcctsAesSymEncryption_IV"] ?? throw new InvalidOperationException("AES IV is not configured.");

            string decryptedOwnerId = _encryptionHelper.Decrypt(requestDTO.EncryptedOwnerId, aesKey, aesIV);

            if (decryptedOwnerId == null)
            {
                accountStatusResponse.IsSuccess = false;
                accountStatusResponse.Errors.Add("Unable to validate credentials.");
                _logger.LogInformation("*** Unable to obtain valid user credentials from decrypted OwnerId. ***"); 
                return accountStatusResponse;
            }
            else
            {
                _logger.LogInformation("*** Decrypted OwnerId successfully obtained using RSA decryption keys. ***"); 
                Account? account = await _accounts.Find(a => a.OwnerId == decryptedOwnerId).FirstOrDefaultAsync();

                accountStatusResponse.Status = account != null ? account.AccountStatus : null;
                if (account is null) accountStatusResponse.Errors.Add("Account not found.");
                Address? billingAddress = account?.Addresses.First(a => a.IsPrimaryShipping == true);
                Address? shippingAddress = account?.Addresses.First(a => a.IsPrimaryShipping == true);
                if (billingAddress is null) accountStatusResponse.Errors.Add("Billing address not found.");
                else accountStatusResponse.BillingAddress = _mapper.MapAddressToDTO(billingAddress);
                if (shippingAddress is null) accountStatusResponse.Errors.Add("Shipping address not found.");
                else accountStatusResponse.ShippingAddress = _mapper.MapAddressToDTO(shippingAddress);

                if (accountStatusResponse.Errors.Count > 0) accountStatusResponse.IsSuccess = false;
                else accountStatusResponse.IsSuccess = true;
                return accountStatusResponse;
            }
        }
    }
}
