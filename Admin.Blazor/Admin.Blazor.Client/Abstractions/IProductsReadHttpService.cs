using Admin.Blazor.Client.DTOs.Claims;

namespace Admin.Blazor.Client.Abstractions
{
    public interface IProductsReadHttpService
    {
        // Dev Tests
        Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetProductsReadApiUserInfoAsync();
        Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken);
    }
}
