using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Claims;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Products;
using Admin.Blazor.Client.DTOs.Products.Test;
using Admin.Blazor.Client.ErrorHandling;
using Admin.Blazor.Client.Paging;
using Admin.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text;
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

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/healthClient";

            try
            {
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
            
                response.EnsureSuccessStatusCode();

                var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
                if (resultDTO is not null)
                {
                    _logger.LogInformation("ProductsReadApiService CheckHealthAsync() at path '{uri}' Result: \n{resultDTO}", uri, JsonSerializer.Serialize(resultDTO));
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError("ProductsReadApiService CheckHealthAsync() at path '{uri}' Exception: {ex.Message}", uri, ex.Message);
                return (false, null, $"Exception: {ex.Message}");
            }

        }

        public async IAsyncEnumerable<ProductDTO> StreamProductsAsync()
        {
            // Note: by default request is configured for HttpCompletionOption.ResponseContentRead - could not determin how to set it to ResponseHeadersRead which is needed for streaming.
            // So, currently the entire response content is read before we start deserializing and streaming the products.
            // This is not ideal for large datasets.
            // In real implementation, we would want to set HttpCompletionOption to ResponseHeadersRead to start processing the stream as it comes in,
            // but that would require a different approach than using IDownstreamApi abstraction,
            // such as using HttpClient directly and configuring it for streaming.

            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/productStream";

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
                    // options.HttpCompletionOption = ResponseHeadersRead; // to facilitate streaming
                });

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();

            var products = JsonSerializer.DeserializeAsyncEnumerable<ProductDTO>(responseStream, _jsonSerializerOptions);

            await foreach (var product in products)
            {
                _logger.LogInformation("Streamed product: {Name}", product?.Name);
                yield return product!;
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, string? ErrorMessage)> GetProductsAsync()
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}";

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

        public async Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? ProductSummaries, string? ErrorMessage)> GetProductSummariesAsync()
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/summaries";

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

        public async Task<(bool IsSuccess, IEnumerable<ProductDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductsAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/paged?filter={filter}&category={category}&sortColumn={sortColumn}&pageNumber={pageNumber}&pageSize={pageSize}";

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

        public async Task<(bool IsSuccess, IEnumerable<ProductSummaryDTO>? Products, PaginationMetadata? PagingData, DateTime? FetchTime, string? ErrorMessage)> GetPagedAndFilteredProductSummariesAsync(
            string? filter, string? category, string? sortColumn, int pageNumber = 1, int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/paged/summaries?filter={filter}&category={category}&sortColumn={sortColumn}&pageNumber={pageNumber}&pageSize={pageSize}";

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

        public async Task<(bool IsSuccess, ProductDTO? Product, string? ErrorMessage)> GetProductByIdAsync(int id)
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/{id}";

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

        public async Task<(bool IsSuccess, ProductSummaryDTO? ProductSummary, string? ErrorMessage)> GetProductSummaryByIdAsync(int id)
        {
            string uri = $"{StaticData.ProductsReadApiService_ProductsPath}/summary/{id}";

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
                string result = await response.Content.ReadAsStringAsync();
                ProductSummaryDTO? productSummary = JsonSerializer.Deserialize<ProductSummaryDTO>(result, _jsonSerializerOptions);
                // Console.WriteLine(productSummary);
                return (true, productSummary, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        //// Use Dev Tests Path for the following methods

        /// </summary>
        /// <param name="throwExceptionDTO"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsReadApiService_DevTestsPath}/throwExceptionForTesting";
            var stringContent = new StringContent(JsonSerializer.Serialize(throwExceptionDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsReadApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "POST";
                    options.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    };
                    options.RelativePath = uri;
                },
                user: null,
                content: stringContent);

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
