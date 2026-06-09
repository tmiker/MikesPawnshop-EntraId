using Accounts.API.DTOs;

namespace Accounts.API.Abstractions
{
    public interface IAccountService
    {
        Task<(bool IsSuccess, int AccountCount, string? ErrorMessage)> GetAccountCountAsync();
        Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountByOwnerIdAsync(string ownerId);
        Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountByAccountIdAsync(string accountId);
        Task<(bool IsSuccess, string? ErrorMessage)> CreateAccountAsync(string ownerId, AddAccountDTO addAccountDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> AddAddressAsync(string ownerId, AddAddressDTO addAddressDTO);
    }
}
