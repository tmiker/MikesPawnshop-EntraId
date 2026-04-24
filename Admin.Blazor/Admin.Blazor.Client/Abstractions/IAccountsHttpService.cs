using Admin.Blazor.Client.DTOs.Accounts;
using Admin.Blazor.Client.DTOs.Health;

namespace Admin.Blazor.Client.Abstractions
{
    public interface IAccountsHttpService
    {
        Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync();
        Task<(bool IsSuccess, string? ErrorMessage)> AccountIsEstablishedAsync();
        Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountAsync();
        Task<(bool IsSuccess, string? ErrorMessage)> CreateAccountAsync(AddAccountDTO addAccountDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> AddAddressAsync(AddAddressDTO addAddressDTO);
    }
}
