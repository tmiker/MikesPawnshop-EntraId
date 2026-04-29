using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Products;
using Admin.Blazor.Client.DTOs.Products.Test;
using Admin.Blazor.Client.ErrorHandling;
using Admin.Blazor.Client.Paging;
using Admin.Blazor.Client.Utility;
using System.Text;
using System.Text.Json;

namespace Admin.Blazor.HttpServices
{
    public class ProductsReadHttpService : IPublicProductsReadHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductsReadHttpService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public ProductsReadHttpService(IHttpClientFactory httpClientFactory, ILogger<ProductsReadHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/healthClient";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            //HttpClient client = new HttpClient();
            //string uri = $"https://localhost:7101/api/products/health"; // 7101

            //string uri = $"{StaticData.AccountsHttpClient_AccountsPath}/health";
            //var client = _httpClientFactory.CreateClient(StaticData.AccountsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            //string uriDirect = "https://localhost:7101/api/products/health";
            //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uriDirect);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();

                //string json = await response.Content.ReadAsStringAsync();
                //if (string.IsNullOrWhiteSpace(json)) return (false, null, $"The API returned no content.");
                //_logger.LogInformation("JSON HEALTH CHECK: {json}", json);

                var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
                if (resultDTO is not null)
                {
                    _logger.LogInformation($"ProductsReadHttpService CheckHealthAsync() at path '{request.RequestUri}' Result: \n{JsonSerializer.Serialize(resultDTO)}");
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProductsReadHttpService CheckHealthAsync() at path '{request.RequestUri}' Exception: {ex.Message}");
                return (false, null, $"Exception: {ex.Message}");
            }
            //try
            //{
            //    response.EnsureSuccessStatusCode();

            //    string resultString = await response.Content.ReadAsStringAsync();
            //    if (resultString is null || resultString.Length == 0) return (false, null, "WTF! The result is null or empty.");

            //    var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
            //    if (resultDTO is not null)
            //    {
            //        _logger.LogInformation($"ProductsReadHttpService CheckHealthAsync() at path \'{request.RequestUri}\' Result: \n{JsonSerializer.Serialize(resultDTO)}");
            //        return (true, resultDTO, null);
            //    }
            //    else return (false, null, "Health check result DTO is null.");
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"ProductsReadHttpService CheckHealthAsync() at path '{request.RequestUri}' Exception: {ex.Message}");
            //    return (false, null, ex.Message);
            //}
        }

        public async IAsyncEnumerable<ProductDTO> StreamProductsAsync()
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/productStream";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var responseStream = await response.Content.ReadAsStreamAsync();

                var products = JsonSerializer.DeserializeAsyncEnumerable<ProductDTO>(responseStream);

                await foreach (var product in products)
                {
                    Console.WriteLine(product?.Name);
                    yield return product!;
                }
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, string? ErrorMessage)> GetProductsAsync()
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    IEnumerable<ProductDTO>? products = JsonSerializer.Deserialize<IEnumerable<ProductDTO>>(result, _jsonSerializerOptions);
                    Console.WriteLine(products);
                    return (true, products, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, null, errorMessage);
                }
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? ProductSummaries, string? ErrorMessage)> GetProductSummariesAsync()
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/summaries";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"SUCCESS GETTING PRODUCT SUMMARIES.");
                    string result = await response.Content.ReadAsStringAsync();
                    IEnumerable<ProductSummaryDTO>? productSummaries = JsonSerializer.Deserialize<IEnumerable<ProductSummaryDTO>>(result, _jsonSerializerOptions);
                    foreach (var summary in productSummaries!)
                    {
                        Console.WriteLine(summary.Name);
                    }
                    return (true, productSummaries, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, null, errorMessage);
                }
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductsAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/paged?filter={filter}&category={category}&sortColumn={sortColumn}&pageNumber={pageNumber}&pageSize={pageSize}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    PagedProductsDTO? pagedProducts = JsonSerializer.Deserialize<PagedProductsDTO>(result, _jsonSerializerOptions);
                    Console.WriteLine(pagedProducts);
                    return (true, pagedProducts?.Products, pagedProducts?.PagingData, pagedProducts?.FetchTime, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, null, null, null, errorMessage);
                }
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductSummariesAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/paged/summaries?filter={filter}&category={category}&sortColumn={sortColumn}&pageNumber={pageNumber}&pageSize={pageSize}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    PagedProductSummariesDTO? pagedProductSummaries = JsonSerializer.Deserialize<PagedProductSummariesDTO>(result, _jsonSerializerOptions);
                    Console.WriteLine(pagedProductSummaries);
                    return (true, pagedProductSummaries?.ProductSummaries, pagedProductSummaries?.PagingData, pagedProductSummaries?.FetchTime, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, null, null, null, errorMessage);
                }
            }
        }

        public async Task<(bool IsSuccess, ProductDTO? Product, string? ErrorMessage)> GetProductByIdAsync(int id)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/{id}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();
                    //string result = await response.Content.ReadAsStringAsync();
                    //ProductDTO? product = JsonSerializer.Deserialize<ProductDTO>(result, _jsonOptions);
                    //Console.WriteLine(product);
                    return (true, product, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, null, errorMessage);
                }
            }
        }

        public async Task<(bool IsSuccess, ProductSummaryDTO? ProductSummary, string? ErrorMessage)> GetProductSummaryByIdAsync(int id)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/summary/{id}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    ProductSummaryDTO? productSummary = JsonSerializer.Deserialize<ProductSummaryDTO>(result, _jsonSerializerOptions);
                    Console.WriteLine(productSummary);
                    return (true, productSummary, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, null, errorMessage);
                }
            }
        }

        // Dev Tests
        public async Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_DevTestsPath}/throwExceptionForTesting";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(throwExceptionDTO), Encoding.UTF8, "application/json");

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("The action ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO) returned " +
                        "HttptatusCode success. It should return Problem Details.");
                    return (true, null);
                }
                else
                {
                    string errorMessage = await GetErrorMessageAsync(response);
                    return (false, errorMessage);
                }
            }
        }

        public async Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_DevTestsPath}/getCloudAmqpSettingsTestingDummyValue";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
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
        }

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
            {
                CustomProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<CustomProblemDetails>();
                string? traceId = problemDetails?.Extensions?["traceId"]?.ToString();
                string? correlationId = problemDetails?.Extensions?["correlationId"]?.ToString();
                string? title = problemDetails?.Title;
                string? detail = problemDetails?.Detail;

                return problemDetails?.ToString()!;
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
