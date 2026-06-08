using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.Utility;

namespace Admin.Blazor.HttpServices
{
    public class AzureServicesHttpClient : IAzureServicesHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public AzureServicesHttpClient(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckAccountsApiAsync()
        {
            var baseUrl = _config["AccountsApiBaseURL"];
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{baseUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return (true, result, null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Accounts API.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Accounts API Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckCartsApiAsync()
        {
            var baseUrl = _config["CartsApiBaseURL"];
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{baseUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return (true, result, null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Carts API.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Carts API Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckOrdersApiAsync()
        {
            var baseUrl = _config["OrdersApiBaseURL"];
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{baseUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return (true, result, null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Orders API.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Orders API Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsReadApiAsync()
        {
            var baseUrl = _config["ProductsReadApiBaseURL"];
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{baseUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return (true, result, null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Products Read API.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Products Read API Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsWriteApiAsync()
        {
            var baseUrl = _config["ProductsWriteApiBaseURL"];
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{baseUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return (true, result, null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Products Write API.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Products Write API Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsReadSqlAsync() 
        {
            var baseUrl = _config["ProductsReadApiBaseURL"];
            var productCountUrl = $"{baseUrl}/api/products/productCount";
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{productCountUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string countResult = await response.Content.ReadAsStringAsync();
                        return (true, $"Product Read Side SQL database is running. Product Count: {countResult}", null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Products Read SQL databasse.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Products Read SQL Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsWriteSqlAsync()
        {
            var baseUrl = _config["ProductsWriteApiBaseURL"];
            var eventCountUrl = $"{baseUrl}/dev/productsManagement/eventCount";
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{eventCountUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string countResult = await response.Content.ReadAsStringAsync();
                        return (true, $"Product Write Side SQL database is running. Event Count: {countResult}", null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach Products Write SQL databasse.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Products Write SQL Error: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckYarpProxyAsync()
        {
            var baseUrl = _config["YarpProxyBaseURL"];
            var client = _httpClientFactory.CreateClient(StaticData.AzureServicesHttpClient_ClientName);

            try
            {
                using (var response = await client.GetAsync($"{baseUrl}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return (true, result, null);
                    }
                    else
                    {
                        return (false, null, "Failed to reach YARP Proxy.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"YARP Proxy Error: {ex.Message}");
            }
        }
    }
}
