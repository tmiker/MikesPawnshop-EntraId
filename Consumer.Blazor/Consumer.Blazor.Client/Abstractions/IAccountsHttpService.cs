using Consumer.Blazor.Client.DTOs.Accounts;

namespace Consumer.Blazor.Client.Abstractions
{
    public interface IAccountsHttpService
    {
        Task<(bool IsSuccess, string? ErrorMessage)> AccountIsEstablished();
        Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountAsync();
        Task<(bool IsSuccess, string? ErrorMessage)> CreateAccountAsync(AddAccountDTO addAccountDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> AddAddressAsync(AddAddressDTO addAddressDTO);
    }
}
