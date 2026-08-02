using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Carts;
using Consumer.Blazor.Client.Utility;
using System.Text;
using System.Text.Json;

namespace Consumer.Blazor.HttpServices
{
    public class CartsHttpService : ICartsHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CartsHttpService> _logger;

        public CartsHttpService(IHttpClientFactory httpClientFactory, ILogger<CartsHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, int CartItemCount, string? ErrorMessage)> AddNewCartItemAsync(AddShoppingCartItemDTO addShoppingCartItemDTO)
        {
            string uri = $"{StaticData.CartsHttpClient_CartsPath}/items";
            var client = _httpClientFactory.CreateClient(StaticData.CartsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(addShoppingCartItemDTO), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                int cartItemCount = await response.Content.ReadFromJsonAsync<int>();
                return (true, cartItemCount, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, -1, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> UpdateProductQuantityAsync(string aggregateId, int amount)
        {
            string uri = $"{StaticData.CartsHttpClient_CartsPath}/items?aggregateId={aggregateId}&amount={amount}";
            var client = _httpClientFactory.CreateClient(StaticData.CartsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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
            string uri = $"{StaticData.CartsHttpClient_CartsPath}/items?aggregateId={aggregateId}";
            var client = _httpClientFactory.CreateClient(StaticData.CartsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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

        public async Task<(bool IsSuccess, ShoppingCartDTO? ShoppingCart, string? ErrorMessage)> GetShoppingCartAsync()
        {
            string uri = $"{StaticData.CartsHttpClient_CartsPath}";
            var client = _httpClientFactory.CreateClient(StaticData.CartsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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
            string uri = $"{StaticData.CartsHttpClient_CartsPath}";
            var client = _httpClientFactory.CreateClient(StaticData.CartsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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
