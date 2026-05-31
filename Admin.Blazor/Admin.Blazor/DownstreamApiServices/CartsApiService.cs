using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Carts;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text;
using System.Text.Json;

namespace Admin.Blazor.DownstreamApiServices
{
    public class CartsApiService : ICartsHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<CartsApiService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public CartsApiService(IDownstreamApi downstreamApi, ILogger<CartsApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.CartsApiService_CartsPath}/healthClient";

            try
            {
                var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

                response.EnsureSuccessStatusCode();
                var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
                if (resultDTO is not null)
                {
                    _logger.LogInformation("CartsApiService CheckHealthAsync() at path '{uri}' Result: \n{resultDTO}", uri, JsonSerializer.Serialize(resultDTO));
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError("CartsApiService CheckHealthAsync() at path '{uri}' Exception: {ex.Message}", uri, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, int CartItemCount, string? ErrorMessage)> AddNewCartItemAsync(AddShoppingCartItemDTO addShoppingCartItemDTO)
        {
            string uri = $"{StaticData.CartsApiService_CartsPath}/items";
            var stringContent = new StringContent(JsonSerializer.Serialize(addShoppingCartItemDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "POST";
                    options.RelativePath = uri;
                },
                user: null,
                content: stringContent);

            if (response.IsSuccessStatusCode)
            {
                int cartItemQuantity = await response.Content.ReadFromJsonAsync<int>();
                return (true, cartItemQuantity, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, -1, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> UpdateProductQuantityAsync(string aggregateId, int amount)
        {
            string uri = $"{StaticData.CartsApiService_CartsPath}/items?aggregateId={aggregateId}&amount={amount}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "PUT";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> RemoveCartItemAsync(string aggregateId)
        {
            string uri = $"{StaticData.CartsApiService_CartsPath}/items?aggregateId={aggregateId}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "DELETE";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        // [HttpGet]
        // public async Task<ActionResult<ShoppingCartDTO?>> GetShoppingCart()
        public async Task<(bool IsSuccess, ShoppingCartDTO? ShoppingCart, string? ErrorMessage)> GetShoppingCartAsync()
        {
            string uri = $"{StaticData.CartsApiService_CartsPath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                ShoppingCartDTO? cartDTO = await response.Content.ReadFromJsonAsync<ShoppingCartDTO>();
                return (true, cartDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> RemoveShoppingCartAsync()
        {
            string uri = $"{StaticData.CartsApiService_CartsPath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.CartsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "DELETE";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
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
