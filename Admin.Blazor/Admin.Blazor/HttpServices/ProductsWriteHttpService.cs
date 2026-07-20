using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Claims;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Products.Test;
using Admin.Blazor.Client.DTOs.Products.Write;
using Admin.Blazor.Client.ErrorHandling;
using Admin.Blazor.Client.Paging;
using Admin.Blazor.Client.Utility;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Admin.Blazor.HttpServices
{
    public class ProductsWriteHttpService : IProductsWriteHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductsWriteHttpService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public ProductsWriteHttpService(IHttpClientFactory httpClientFactory, ILogger<ProductsWriteHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/healthClient";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();

                var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
                if (resultDTO is not null)
                {
                    _logger.LogInformation($"ProductsWriteHttpService CheckHealthAsync() at path '{request.RequestUri}' Result: \n{JsonSerializer.Serialize(resultDTO)}");
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProductsWriteHttpService CheckHealthAsync() at path '{request.RequestUri}' Exception: {ex.Message}");
                return (false, null, $"Exception: {ex.Message}");

            }
        }

        public async Task<(bool IsSuccess, int EventCount, string? ErrorMessage)> GetProductEventRecordCountAsync()
        {
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/eventCount";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                if (int.TryParse(result, out int parsedCount))
                {
                    return (true, parsedCount, null);
                }
                return (true, -1, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, 0, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetProductsWriteApiUserInfoAsync(string? token = null)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}{StaticData.ProductsWriteHttpClient_GetApiUserInfoSubpath}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                ApiUserInfoDTO? apiUserInfoDTO = await response.Content.ReadFromJsonAsync<ApiUserInfoDTO>();
                return (true, apiUserInfoDTO, null);
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (false, new ApiUserInfoDTO() { ErrorMessage = $"Access denied." }, "Access denied.");
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, new ApiUserInfoDTO() { ErrorMessage = errorMessage }, errorMessage);
            }
        }

        // Write Products path
        public async Task<(bool IsSuccess, Guid? AggregateId, string? ErrorMessage)> AddProductAsync(AddProductDTO addProductDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            
            request.Content = new StringContent(JsonSerializer.Serialize(addProductDTO), Encoding.UTF8, "application/json");

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                AddProductResult? result = await response.Content.ReadFromJsonAsync<AddProductResult>();
                return (true, result?.ProductId, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> UpdateStatusAsync(UpdateStatusDTO updateStatusDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/status";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
            
            request.Content = new StringContent(JsonSerializer.Serialize(updateStatusDTO), Encoding.UTF8, "application/json");

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddImageAsync(AddImageDTO addImageDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/image";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            
            request.Content = new StringContent(JsonSerializer.Serialize(addImageDTO), Encoding.UTF8, "application/json");

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddDocumentAsync(AddDocumentDTO addDocumentDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/document";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

            request.Content = new StringContent(JsonSerializer.Serialize(addDocumentDTO), Encoding.UTF8, "application/json");

            // Generate a new Correlation ID and add to headers
            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        // UPDATED METHODS FOR IMAGES AND DOCUMENTS
        public async Task<(bool IsSuccess, string? ErrorMessage)> AddProductImageAsync(AddImageDTO addImageDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/image";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            // build form file to submit to api endpoint
            using (var content = new MultipartFormDataContent())
            {
                //no Id for add
                if (!string.IsNullOrWhiteSpace(addImageDTO.ProductId)) content.Add(new StringContent(addImageDTO.ProductId!), nameof(addImageDTO.ProductId));
                if (!string.IsNullOrWhiteSpace(addImageDTO.Name)) content.Add(new StringContent(addImageDTO.Name!), nameof(addImageDTO.Name));
                if (!string.IsNullOrWhiteSpace(addImageDTO.Caption)) content.Add(new StringContent(addImageDTO.Caption!), nameof(addImageDTO.Caption));
                // content.Add(new StringContent(addImageDTO.SequenceNumber.ToString()), nameof(addImageDTO.SequenceNumber));
                if (!string.IsNullOrWhiteSpace(addImageDTO.BlobFileName)) content.Add(new StringContent(addImageDTO.BlobFileName!), nameof(addImageDTO.BlobFileName));
                if (addImageDTO.ImageBlob is not null)
                {
                    var image = addImageDTO.ImageBlob;
                    var fileContent = new StreamContent(image.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    content.Add(content: fileContent, name: "ImageBlob", fileName: addImageDTO.ImageBlob.Name);

                    request.Content = content;

                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode) return (true, null);
                    else
                    {
                        string errorMessage = await GetErrorMessageAsync(response);
                        return (false, errorMessage);
                    }
                }

                else return (false, "No image was provided.");
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddProductDocumentAsync(AddDocumentDTO addDocumentDTO, CancellationToken cancellationToken)
        {
            if (addDocumentDTO!.DocumentBlob is not null) Console.WriteLine($"The HTTP CLIENT Document Blob IS NOT null.");
            else Console.WriteLine($"The HTTP CLIENT Document Blob IS null.");

            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/document";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            // build form file to submit to api endpoint
            using (var content = new MultipartFormDataContent())
            {
                //no Id for add
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.ProductId)) content.Add(new StringContent(addDocumentDTO.ProductId!), nameof(addDocumentDTO.ProductId));
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.Name)) content.Add(new StringContent(addDocumentDTO.Name!), nameof(addDocumentDTO.Name));
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.Title)) content.Add(new StringContent(addDocumentDTO.Title!), nameof(addDocumentDTO.Title));
                // content.Add(new StringContent(addDocumentDTO.SequenceNumber.ToString()), nameof(addDocumentDTO.SequenceNumber));
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.BlobFileName)) content.Add(new StringContent(addDocumentDTO.BlobFileName!), nameof(addDocumentDTO.BlobFileName));
                if (addDocumentDTO.DocumentBlob is not null)
                {
                    IBrowserFile blob = addDocumentDTO.DocumentBlob;
                    var fileContent = new StreamContent(blob.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(blob.ContentType);
                    content.Add(content: fileContent, name: "DocumentBlob", fileName: addDocumentDTO.DocumentBlob.Name);

                    request.Content = content;

                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode) return (true, null);
                    else
                    {
                        string errorMessage = await GetErrorMessageAsync(response);
                        return (false, errorMessage);
                    }
                }
                else return (false, "No document was provided.");
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductImageAsync(DeleteImageDTO deleteImageDTO)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/image";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(deleteImageDTO), Encoding.UTF8, "application/json");

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductDocumentAsync(DeleteDocumentDTO deleteDocumentDTO)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/document";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(deleteDocumentDTO), Encoding.UTF8, "application/json");

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> CheckAzureStorageStatusAsync()
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_ProductsPath}/azureBlobStoragePingTest";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        // Dev Tests
        public async Task<(bool IsSuccess, IEnumerable<ProductSnapshotDTO>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedAndFilteredProductSnapshotsAsync(
            string? aggregateId,
            string? category,
            string? sortColumn,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/pagedAndFilteredProductSnapshots?aggregateId={aggregateId}&category={category}&sortColumn={sortColumn}&pageNumber={pageNumber}&pageSize={pageSize}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            Console.WriteLine($"BLAZOR DEV CLIENT HTTP CLIENT CALL URI: {uri}");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                PagedProductSnapshotResult? result = await response.Content.ReadFromJsonAsync<PagedProductSnapshotResult>();
                return (true, result?.ProductSnapshots, result?.PagingData, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<ProductSnapshotDTO>? ProductSnapshots, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedProductSnapshotsAsync(
            string? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/productSnapshots?aggregateId={aggregateId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                PagedProductSnapshotResult? result = await response.Content.ReadFromJsonAsync<PagedProductSnapshotResult>();
                return (true, result?.ProductSnapshots, result?.PagingData, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, ProductSnapshotDTO? ProductSnapshot, string? ErrorMessage)> GetProductSnapshotByIdAsync(
            string? aggregateId,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/productSnapshot/{aggregateId}?minVersion={minVersion}&maxVersion={maxVersion}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                ProductSnapshotDTO? snapshotDTO = await response.Content.ReadFromJsonAsync<ProductSnapshotDTO>();
                return (true, snapshotDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<EventRecordDTO>? EventRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedEventRecordsAsync(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/eventRecords?aggregateId={aggregateId}&correlationId={correlationId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";
            _logger.LogInformation("GET EVENT RECORDS URI: {uri}", uri);
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                PagedEventRecordResult? result = await response.Content.ReadFromJsonAsync<PagedEventRecordResult>();
                return (true, result?.EventRecords, result?.PagingData, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<OutboxRecordDTO>? OutboxRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedOutboxRecordsAsync(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/outboxRecords?aggregateId={aggregateId}&correlationId={correlationId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                PagedOutboxRecordResult? result = await response.Content.ReadFromJsonAsync<PagedOutboxRecordResult>();
                return (true, result?.OutboxRecords, result?.PagingData, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<SnapshotRecordDTO>? SnapshotRecords, PaginationMetadata? PagingData, string? ErrorMessage)> GetPagedSnapshotRecordsAsync(
            string? aggregateId,
            string? correlationId = null,
            int minVersion = 0,
            int maxVersion = Int32.MaxValue,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/snapshotRecords?aggregateId={aggregateId}&correlationId={correlationId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                PagedSnapshotRecordResult? result = await response.Content.ReadFromJsonAsync<PagedSnapshotRecordResult>();
                return (true, result?.SnapshotRecords, result?.PagingData, null);

            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/throwExceptionForTesting";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
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

        public async Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/getCloudAmqpSettingsTestingDummyValue";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
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

        public async Task<(bool IsSuccess, string? ErrorMessage)> PurgeDataAsync(PurgeDataDTO purgeDataDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/purgeData";
            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(purgeDataDTO), Encoding.UTF8, "application/json");

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductByIdAsync(Guid aggregateId, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteHttpClient_DevTestsPath}/permanentlyDeleteProduct?aggregateId={aggregateId}";

            var client = _httpClientFactory.CreateClient(StaticData.ProductsWriteHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);

            string correlationId = Guid.NewGuid().ToString();
            request.Headers.Add("X-Correlation-ID", correlationId);

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
