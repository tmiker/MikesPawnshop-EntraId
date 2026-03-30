using Accounts.API.Abstractions;
using Accounts.API.Domain.Models;
using Accounts.API.DTOs;
using MongoDB.Driver;

namespace Accounts.API.Services
{
    public class AccountService : IAccountService
    {
        private readonly IMongoCollection<Account> _accounts;
        private readonly IAccountDataMapper _mapper;
        private readonly ILogger<AccountService> _logger;

        private const int _baseCreditLimit = 5000;

        public AccountService(IMongoSettings mongoSettings, IAccountDataMapper mapper, ILogger<AccountService> logger)
        {
            var client = new MongoClient(mongoSettings.MongoLocalConnection);
            var database = client.GetDatabase(mongoSettings.Database);
            _accounts = database.GetCollection<Account>(mongoSettings.AccountCollection);
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountByOwnerIdAsync(string ownerId)
        {
            Account? account = await _accounts.Find(a => a.OwnerId == ownerId).FirstOrDefaultAsync();
            if (account == null) return (false, null, $"Account not found.");
            AccountDTO accountDTO = _mapper.MapAccountToDTO(account);
            return (true, accountDTO, null);
        }

        public async Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountByAccountIdAsync(string accountId)
        {
            Account? account = await _accounts.Find(a => a.AccountId == accountId).FirstOrDefaultAsync();
            if (account == null) return (false, null, $"Account not found.");
            AccountDTO accountDTO = _mapper.MapAccountToDTO(account);
            return (true, accountDTO, null);
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> CreateAccountAsync(string ownerId, AddAccountDTO addAccountDTO)
        {
            Account account = new Account()
            {
                AccountId = Guid.NewGuid().ToString(),
                OwnerId = ownerId,
                FirstName = addAccountDTO.FirstName,
                LastName = addAccountDTO.LastName,
                Email = addAccountDTO.Email,
                Addresses = addAccountDTO.Addresses.Select(addressDTO => new Address()
                {
                    Street1 = addressDTO.Street1,
                    Street2 = addressDTO.Street2,
                    City = addressDTO.City,
                    State = addressDTO.State,
                    PostalCode = addressDTO.PostalCode,
                    IsPrimaryBilling = addressDTO.IsPrimaryBilling,
                    IsPrimaryShipping = addressDTO.IsPrimaryShipping
                }).ToList(),
                PhoneNumber = addAccountDTO.PhoneNumber,
                AccountStatus = "Active",                       // use 'Hold' for accountstatus to prevent placing of orders
                CreditLimit = _baseCreditLimit
            };
            await _accounts.InsertOneAsync(account);
            return (true, null);
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddAddressAsync(string ownerId, AddAddressDTO addAddressDTO)
        {
            Account account = await _accounts.Find(a => a.OwnerId == ownerId).FirstOrDefaultAsync();
            Address address = new Address()     
            {
                Street1 = addAddressDTO.Street1,
                Street2 = addAddressDTO.Street2,
                City = addAddressDTO.City,
                State = addAddressDTO.State,
                PostalCode = addAddressDTO.PostalCode,
                IsPrimaryBilling = addAddressDTO.IsPrimaryBilling,
                IsPrimaryShipping = addAddressDTO.IsPrimaryShipping
            };

            bool changed = account.AddAddress(address);
            if (changed)
            {
                await _accounts.ReplaceOneAsync(a => a.OwnerId == ownerId, account);
                return (true, null);
            }
            else return (false, "The address already exists.");
        }
    }
}
