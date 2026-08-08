using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Claims;
using Admin.Blazor.Client.ErrorHandling;
using Admin.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text.Json;

namespace Admin.Blazor.DownstreamApiServices
{
    public class ProductsReadApiService : IProductsReadHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<ProductsReadApiService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public ProductsReadApiService(IDownstreamApi downstreamApi, ILogger<ProductsReadApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        // DEVELOPMENT
        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetProductsReadApiUserInfoAsync()
        {
            string uri = $"{StaticData.ProductsReadApiService_DevTestsPath}{StaticData.ProductsReadApiService_GetApiUserInfoSubpath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsReadApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    };
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

        public async Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsReadApiService_DevTestsPath}/getCloudAmqpSettingsTestingDummyValue";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsReadApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    };
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                string value = await response.Content.ReadAsStringAsync();
                return (true, value, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }


        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
            {
                try
                {
                    ProductsReadProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProductsReadProblemDetails>();
                    return problemDetails?.ToString()!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    var result = await response.Content.ReadAsStringAsync();
                    return result;
                }
            }
            else
            {
                string errorMessage = string.Empty;
                if (!string.IsNullOrEmpty(response.StatusCode.ToString())) errorMessage += $"Status Code: {response.StatusCode.ToString()}; ";
                if (!string.IsNullOrEmpty(response.ReasonPhrase)) errorMessage += $"Reason Phrase: {response.ReasonPhrase}; ";
                string responseContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseContent)) errorMessage += $"\nResponse Content: {responseContent}; ";

                return errorMessage;
            }
        }
    }
}
