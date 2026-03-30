using Development.Blazor.Client.Abstractions;
using Development.Blazor.Client.DTOs;
using Development.Blazor.Client.DTOs.Orders;
using Development.Blazor.Client.Paging;
using Development.Blazor.Client.Utility;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Development.Blazor.HttpProviders
{
    public class OrdersHttpService : IOrdersHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrdersHttpService> _logger;

        public OrdersHttpService(IHttpClientFactory httpClientFactory, ILogger<OrdersHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetOrdersApiUserInfoAsync(string? token = null)
        {
            string uri = $"{StaticData.OrdersHttpClient_DevTestsPath}{StaticData.OrdersHttpClient_GetApiUserInfoSubpath}";
            Debug.WriteLine($"GET API USER INFO URI: {uri}");
            var client = _httpClientFactory.CreateClient(StaticData.OrdersHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            Debug.WriteLine($"GET API USER INFO REQUEST URI: {request.RequestUri}");
            HttpResponseMessage response = await client.SendAsync(request);

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

        public async Task<(bool IsSuccess, ReviewOrderResultDTO? ReviewOrderResult, string? ErrorMessage)> ReviewOrderAsync(AddOrderDTO addOrderDTO, string? token = null)
        {
            string uri = $"{StaticData.OrdersHttpClient_OrdersPath}/reviewOrder";
            var client = _httpClientFactory.CreateClient(StaticData.OrdersHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(addOrderDTO), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.SendAsync(request);

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

        public async Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> SubmitOrderAsync(AddOrderDTO addOrderDTO, string? token = null)
        {
            string uri = $"{StaticData.OrdersHttpClient_OrdersPath}";
            var client = _httpClientFactory.CreateClient(StaticData.OrdersHttpClient_ClientName);
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(addOrderDTO), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.SendAsync(request);

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
            string uri = $"{StaticData.OrdersHttpClient_OrdersPath}?filter={filter}&sortColumn={sortColumn}&sortOrder={sortOrder}&pageNumber={pageNumber}&pageSize={pageSize}";

            var client = _httpClientFactory.CreateClient(StaticData.OrdersHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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
            string uri = $"{StaticData.OrdersHttpClient_OrdersPath}/{orderId}";
            var client = _httpClientFactory.CreateClient(StaticData.OrdersHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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
