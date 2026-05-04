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
        private readonly IRsaAsymmetricKeyContainerManager _rsaKeyContainerManager;
        private readonly IRsaAsymmetricEncryptionManager _rsaEncryptor;
        private readonly ILogger<InternalAccountService> _logger;

        private const int _baseCreditLimit = 5000;

        public InternalAccountService(
            IConfiguration config,
            IMongoSettings mongoSettings, 
            IAccountDataMapper mapper, 
            IRsaAsymmetricKeyContainerManager rsaKeyContainerManager,
            IRsaAsymmetricEncryptionManager rsaEncryptor,
            ILogger<InternalAccountService> logger)
        {
            _config = config;
            string? environment = _config["ASPNETCORE_ENVIRONMENT"];
            var client = environment == "Development" ? new MongoClient(mongoSettings.MongoLocalConnection) : new MongoClient(mongoSettings.AZURE_MONGO_CONNECTION);
            var database = client.GetDatabase(mongoSettings.Database);
            _accounts = database.GetCollection<Account>(mongoSettings.AccountCollection);
            _mapper = mapper;
            _rsaKeyContainerManager = rsaKeyContainerManager;
            _rsaEncryptor = rsaEncryptor;

            _logger = logger;
        }

        public async Task<AccountStatusResponseDTO> GetAccountStatus(AccountStatusRequestDTO requestDTO)
        {
            AccountStatusResponseDTO accountStatusResponse = new AccountStatusResponseDTO();

            if (string.IsNullOrWhiteSpace(requestDTO.EncryptedOwnerId) || string.IsNullOrWhiteSpace(requestDTO.KeyContainerName))
            {
                accountStatusResponse.IsSuccess = false;
                accountStatusResponse.Errors.Add("Missing Required Request Data.");
                return accountStatusResponse;
            }

            // GET KEYS FOR DECRYPTION OF ENCRYPTED OWNERID 
            string publicAndPrivateKeys = _rsaKeyContainerManager.GetPublicAndPrivateKeyForContainerWithName(requestDTO.KeyContainerName);
            // _logger.LogInformation("*** {this}: Public and Private Keys retrieved using RSA key container. Keys: {keys} ***", this.GetType().Name, publicAndPrivateKeys);   // *** DEV ONLY REMOVE *** //
            _logger.LogInformation("*** {this}: Public and Private Keys retrieved using RSA key container named: {keycontainername} ***", this.GetType().Name, requestDTO.KeyContainerName);

            // DECRYPT ENCRYPTED OWNERID 
            string decryptedOwnerId = _rsaEncryptor.DecryptUsingRsaXmlString(requestDTO.EncryptedOwnerId, publicAndPrivateKeys);
            // _logger.LogInformation("*** {this}: Decrypted OwnerId using RSA decryption keys. Decrypted OwnerId: {did} ***", this.GetType().Name, decryptedOwnerId); // *** DEV ONLY REMOVE *** //

            // DELETE KEYS AND CONTAINER TO CLEAN UP RESOURCES AS NO LONGER NEEDED
            _rsaKeyContainerManager.DeleteKeyFromContainer(requestDTO.KeyContainerName);

            if (decryptedOwnerId == null)
            {
                accountStatusResponse.IsSuccess = false;
                accountStatusResponse.Errors.Add("Unable to validate credentials.");
                _logger.LogInformation("*** {this}: Unable to obtain valid user credentials from decrypted OwnerId using RSA decryption keys. ***", this.GetType().Name); 
                return accountStatusResponse;
            }
            else
            {
                _logger.LogInformation("*** {this}: Decrypted OwnerId successfully obtained using RSA decryption keys. ***", this.GetType().Name); 

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
