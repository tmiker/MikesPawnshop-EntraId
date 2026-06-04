using Orders.API.DTOs;

namespace Orders.API.Abstractions
{
    public interface IInternalAccountsHttpService
    {
        Task<AccountStatusResponseDTO> GetUserAccountStatusAsync(AccountStatusRequestDTO accountStatusRequestDTO, CancellationToken? cancellationToken = null);
    }
}
