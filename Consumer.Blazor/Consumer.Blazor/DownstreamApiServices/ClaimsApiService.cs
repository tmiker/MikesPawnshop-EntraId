using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Claims;
using Consumer.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;

namespace Consumer.Blazor.DownstreamApiServices
{
    public class ClaimsApiService : IClaimsHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<ClaimsApiService> _logger;

        public ClaimsApiService(IDownstreamApi downstreamApi, ILogger<ClaimsApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetAccountsApiUserInfoAsync()
        {
            string uri = $"{StaticData.AccountsApiService_DevTestsPath}{StaticData.AccountsApiService_GetApiUserInfoSubpath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.AccountsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                ApiUserInfoDTO? apiUserInfoDTO = await response.Content.ReadFromJsonAsync<ApiUserInfoDTO>();
                return (true, apiUserInfoDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, new ApiUserInfoDTO() { ErrorMessage = errorMessage }, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetCartsApiUserInfoAsync()
        {
            string uri = $"{StaticData.CartsApiService_DevTestsPath}{StaticData.CartsApiService_GetApiUserInfoSubpath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                ApiUserInfoDTO? apiUserInfoDTO = await response.Content.ReadFromJsonAsync<ApiUserInfoDTO>();
                return (true, apiUserInfoDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, new ApiUserInfoDTO() { ErrorMessage = errorMessage }, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetOrdersApiUserInfoAsync()
        {
            string uri = $"{StaticData.OrdersApiService_DevTestsPath}{StaticData.OrdersApiService_GetApiUserInfoSubpath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.OrdersApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                ApiUserInfoDTO? apiUserInfoDTO = await response.Content.ReadFromJsonAsync<ApiUserInfoDTO>();
                return (true, apiUserInfoDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, new ApiUserInfoDTO() { ErrorMessage = errorMessage }, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetProductsReadApiUserInfoAsync()
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}{StaticData.ProductsReadApiService_GetApiUserInfoSubpath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsReadApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                ApiUserInfoDTO? apiUserInfoDTO = await response.Content.ReadFromJsonAsync<ApiUserInfoDTO>();
                return (true, apiUserInfoDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, new ApiUserInfoDTO() { ErrorMessage = errorMessage }, errorMessage);
            }
        }

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            string errorMessage = string.Empty;
            if (!string.IsNullOrEmpty(response.StatusCode.ToString())) errorMessage += $"Status Code: {response.StatusCode.ToString()}; ";
            if (!string.IsNullOrEmpty(response.ReasonPhrase)) errorMessage += $"Reason Phrase: {response.ReasonPhrase}; ";
            string responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseContent)) errorMessage += $"Response Content: {responseContent}; ";
            return errorMessage;
        }
    }
}
