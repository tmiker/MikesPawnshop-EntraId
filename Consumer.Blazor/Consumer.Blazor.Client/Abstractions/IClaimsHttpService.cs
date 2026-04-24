using Consumer.Blazor.Client.DTOs.Claims;

namespace Consumer.Blazor.Client.Abstractions
{
    public interface IClaimsHttpService
    {
        Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetAccountsApiUserInfoAsync();
        Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetCartsApiUserInfoAsync();
        Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetOrdersApiUserInfoAsync();
        Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetProductsReadApiUserInfoAsync();
    }
}
