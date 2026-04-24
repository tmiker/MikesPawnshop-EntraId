using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Accounts;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Orders;
using Admin.Blazor.Client.Paging;
using Admin.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text;
using System.Text.Json;

namespace Admin.Blazor.DownstreamApiServices
{
    public class OrdersApiService : IOrdersHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<OrdersApiService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public OrdersApiService(IDownstreamApi downstreamApi, ILogger<OrdersApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.OrdersApiService_OrdersPath}/healthClient";

            try
            {
                var response = await _downstreamApi.CallApiForUserAsync(
                    serviceName: StaticData.OrdersApiService_ServiceName,
                    downstreamApiOptionsOverride: options =>
                    {
                        options.HttpMethod = "GET";
                        options.RelativePath = uri;
                    });

                response.EnsureSuccessStatusCode();
                var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
                if (resultDTO is not null)
                {
                    _logger.LogInformation("OrdersApiService CheckHealthAsync() at path '{uri}' Result: \n{resultDTO}", uri, JsonSerializer.Serialize(resultDTO));
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError("OrdersApiService CheckHealthAsync() at path '{uri}' Exception: {ex.Message}", uri, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<AccountStatusResponseDTO> GetAccountStatusAsync()
        {
            string uri = $"{StaticData.OrdersApiService_OrdersPath}/accountStatus";

            var response = await _downstreamApi.CallApiForUserAsync(
                    serviceName: StaticData.OrdersApiService_ServiceName,
                    downstreamApiOptionsOverride: options =>
                    {
                        options.HttpMethod = "GET";
                        options.RelativePath = uri;
                    });

            if (response.IsSuccessStatusCode)
            {
                AccountStatusResponseDTO? accountStatusResponse = await response.Content.ReadFromJsonAsync<AccountStatusResponseDTO>();
                return accountStatusResponse ?? new AccountStatusResponseDTO() { Errors = new List<string>() { "Account status response was not received." } };
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return new AccountStatusResponseDTO() { Errors = new List<string>() { errorMessage } };
            }
        }

        public async Task<(bool IsSuccess, ReviewOrderResultDTO? ReviewOrderResult, string? ErrorMessage)> ReviewOrderAsync()
        {
            string uri = $"{StaticData.OrdersApiService_OrdersPath}/reviewOrder";

            var response = await _downstreamApi.CallApiForUserAsync(
                    serviceName: StaticData.OrdersApiService_ServiceName,
                    downstreamApiOptionsOverride: options =>
                    {
                        options.HttpMethod = "GET";
                        options.RelativePath = uri;
                    });

            if (response.IsSuccessStatusCode)
            {
                ReviewOrderResultDTO? reviewOrderResult = await response.Content.ReadFromJsonAsync<ReviewOrderResultDTO>();
                return (true, reviewOrderResult, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> SubmitOrderAsync(AddOrderDTO addOrderDTO)
        {
            string uri = $"{StaticData.OrdersApiService_OrdersPath}";
            var stringContent = new StringContent(JsonSerializer.Serialize(addOrderDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                    serviceName: StaticData.OrdersApiService_ServiceName,
                    downstreamApiOptionsOverride: options =>
                    {
                        options.HttpMethod = "POST";
                        options.RelativePath = uri;
                    },
                    user: null,
                    content: stringContent);

            if (response.IsSuccessStatusCode)
            {
                string? orderId = await response.Content.ReadFromJsonAsync<string>();
                return (true, orderId, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<OrderDTO>? OrderDTOs, PaginationMetadata? PagingData, string? ErrorMessage)> GetAllUserOrdersAsync(
            string? filter = null, string? sortColumn = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10)
        {
            string uri = $"{StaticData.OrdersApiService_OrdersPath}?filter={filter}&sortColumn={sortColumn}&sortOrder={sortOrder}&pageNumber={pageNumber}&pageSize={pageSize}";

            var response = await _downstreamApi.CallApiForUserAsync(
                    serviceName: StaticData.OrdersApiService_ServiceName,
                    downstreamApiOptionsOverride: options =>
                    {
                        options.HttpMethod = "GET";
                        options.RelativePath = uri;
                    });

            if (response.IsSuccessStatusCode)
            {
                PagedOrderResultDTO? pagedResultDTO = await response.Content.ReadFromJsonAsync<PagedOrderResultDTO>();
                return (true, pagedResultDTO?.OrderDTOs, pagedResultDTO?.PagingData, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, OrderDTO? OrderDTO, string? ErrorMessage)> GetOrderByOrderIdAsync(string orderId)
        {
            string uri = $"{StaticData.OrdersApiService_OrdersPath}/{orderId}";

            var response = await _downstreamApi.CallApiForUserAsync(
                    serviceName: StaticData.OrdersApiService_ServiceName,
                    downstreamApiOptionsOverride: options =>
                    {
                        options.HttpMethod = "GET";
                        options.RelativePath = uri;
                    });

            if (response.IsSuccessStatusCode)
            {
                OrderDTO? orderDTO = await response.Content.ReadFromJsonAsync<OrderDTO>();
                return (true, orderDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
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
