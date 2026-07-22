using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Products;
using Consumer.Blazor.Client.DTOs.Products.Test;
using Consumer.Blazor.Client.ErrorHandling;
using Consumer.Blazor.Client.Paging;
using Consumer.Blazor.Client.Utility;
using System.Text;
using System.Text.Json;

namespace Consumer.Blazor.HttpServices
{
    public class ProductsReadHttpService : IPublicProductsReadHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductsReadHttpService> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

        public ProductsReadHttpService(IHttpClientFactory httpClientFactory, ILogger<ProductsReadHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async IAsyncEnumerable<ProductDTO> StreamProductsAsync()
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/productStream";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

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

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    IEnumerable<ProductDTO>? products = JsonSerializer.Deserialize<IEnumerable<ProductDTO>>(result, _jsonOptions);
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

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"SUCCESS GETTING PRODUCT SUMMARIES.");
                    string result = await response.Content.ReadAsStringAsync();
                    IEnumerable<ProductSummaryDTO>? productSummaries = JsonSerializer.Deserialize<IEnumerable<ProductSummaryDTO>>(result, _jsonOptions);
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

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    PagedProductsDTO? pagedProducts = JsonSerializer.Deserialize<PagedProductsDTO>(result, _jsonOptions);
                    // Console.WriteLine(pagedProducts);
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

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    PagedProductSummariesDTO? pagedProductSummaries = JsonSerializer.Deserialize<PagedProductSummariesDTO>(result, _jsonOptions);
                    // Console.WriteLine(pagedProductSummaries);
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

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    ProductSummaryDTO? productSummary = JsonSerializer.Deserialize<ProductSummaryDTO>(result, _jsonOptions);
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

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
            {
                CustomProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<CustomProblemDetails>();
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

        // Dev Tests
        public async Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsReadHttpClient_ProductsPath}/throwExceptionForTesting";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsReadHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(throwExceptionDTO), Encoding.UTF8, "application/json");

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("The action ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO) returned " +
                    "HttptatusCode success. It should return Problem Details.");
                return (true, null);
            }
            else
            {
                string error = await GetErrorMessageAsync(response);
                return (false, error);
            }
        }
    }
}
