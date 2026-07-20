using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Claims;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.DTOs.Products.Test;
using Admin.Blazor.Client.DTOs.Products.Write;
using Admin.Blazor.Client.ErrorHandling;
using Admin.Blazor.Client.Paging;
using Admin.Blazor.Client.Utility;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Identity.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Admin.Blazor.DownstreamApiServices
{
    public class ProductsWriteApiService : IProductsWriteHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<ProductsWriteApiService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public ProductsWriteApiService(IDownstreamApi downstreamApi, ILogger<ProductsWriteApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/healthClient";

            try
            {
                var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
                    _logger.LogInformation("ProductsWriteApiService CheckHealthAsync() at path '{uri}' Result: \n{resultDTO}", uri, JsonSerializer.Serialize(resultDTO));
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError("ProductsWriteHttpService CheckHealthAsync() at path '{uri}' Exception: {ex.Message}", uri, ex.Message);
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, int EventCount, string? ErrorMessage)> GetProductEventRecordCountAsync()
        {
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/eventCount";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}{StaticData.ProductsWriteApiService_GetApiUserInfoSubpath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}";
            var stringContent = new StringContent(JsonSerializer.Serialize(addProductDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/status";
            var stringContent = new StringContent(JsonSerializer.Serialize(updateStatusDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "PUT";
                    options.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    };
                    options.RelativePath = uri;
                },
                user: null,
                content: stringContent);

            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddImageAsync(AddImageDTO addImageDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/image";
            var stringContent = new StringContent(JsonSerializer.Serialize(addImageDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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

            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddDocumentAsync(AddDocumentDTO addDocumentDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/document";
            var stringContent = new StringContent(JsonSerializer.Serialize(addDocumentDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/image";

            // build form file to submit to api endpoint
            using (var mfdContent = new MultipartFormDataContent())
            {
                //no Id for add
                if (!string.IsNullOrWhiteSpace(addImageDTO.ProductId)) mfdContent.Add(new StringContent(addImageDTO.ProductId!), nameof(addImageDTO.ProductId));
                if (!string.IsNullOrWhiteSpace(addImageDTO.Name)) mfdContent.Add(new StringContent(addImageDTO.Name!), nameof(addImageDTO.Name));
                if (!string.IsNullOrWhiteSpace(addImageDTO.Caption)) mfdContent.Add(new StringContent(addImageDTO.Caption!), nameof(addImageDTO.Caption));
                // mfdContent.Add(new StringContent(addImageDTO.SequenceNumber.ToString()), nameof(addImageDTO.SequenceNumber));
                if (!string.IsNullOrWhiteSpace(addImageDTO.BlobFileName)) mfdContent.Add(new StringContent(addImageDTO.BlobFileName!), nameof(addImageDTO.BlobFileName));
                if (addImageDTO.ImageBlob is not null)
                {
                    var image = addImageDTO.ImageBlob;
                    var fileContent = new StreamContent(image.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    mfdContent.Add(content: fileContent, name: "ImageBlob", fileName: addImageDTO.ImageBlob.Name);

                    var response = await _downstreamApi.CallApiForUserAsync(
                        serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
                        content: mfdContent);

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

            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/document";

            // build form file to submit to api endpoint
            using (var mfdContent = new MultipartFormDataContent())
            {
                //no Id for add
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.ProductId)) mfdContent.Add(new StringContent(addDocumentDTO.ProductId!), nameof(addDocumentDTO.ProductId));
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.Name)) mfdContent.Add(new StringContent(addDocumentDTO.Name!), nameof(addDocumentDTO.Name));
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.Title)) mfdContent.Add(new StringContent(addDocumentDTO.Title!), nameof(addDocumentDTO.Title));
                // mfdContent.Add(new StringContent(addDocumentDTO.SequenceNumber.ToString()), nameof(addDocumentDTO.SequenceNumber));
                if (!string.IsNullOrWhiteSpace(addDocumentDTO.BlobFileName)) mfdContent.Add(new StringContent(addDocumentDTO.BlobFileName!), nameof(addDocumentDTO.BlobFileName));
                if (addDocumentDTO.DocumentBlob is not null)
                {
                    IBrowserFile blob = addDocumentDTO.DocumentBlob;
                    var fileContent = new StreamContent(blob.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(blob.ContentType);
                    mfdContent.Add(content: fileContent, name: "DocumentBlob", fileName: addDocumentDTO.DocumentBlob.Name);

                    var response = await _downstreamApi.CallApiForUserAsync(
                        serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
                        content: mfdContent);

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
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/image";
            var stringContent = new StringContent(JsonSerializer.Serialize(deleteImageDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                        serviceName: StaticData.ProductsWriteApiService_ServiceName,
                        downstreamApiOptionsOverride: options =>
                        {
                            options.HttpMethod = "DELETE";
                            options.ExtraHeaderParameters = new Dictionary<string, string>
                            {
                                { "X-Correlation-ID", Guid.NewGuid().ToString() }
                            };
                            options.RelativePath = uri;
                        },
                        user: null,
                        content: stringContent);

            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductDocumentAsync(DeleteDocumentDTO deleteDocumentDTO)
        {
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/document";

            var stringContent = new StringContent(JsonSerializer.Serialize(deleteDocumentDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                        serviceName: StaticData.ProductsWriteApiService_ServiceName,
                        downstreamApiOptionsOverride: options =>
                        {
                            options.HttpMethod = "DELETE";
                            options.ExtraHeaderParameters = new Dictionary<string, string>
                            {
                                { "X-Correlation-ID", Guid.NewGuid().ToString() }
                            };
                            options.RelativePath = uri;
                        },
                        user: null,
                        content: stringContent);

            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> CheckAzureStorageStatusAsync()
        {
            string uri = $"{StaticData.ProductsWriteApiService_ProductsPath}/azureBlobStoragePingTest";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    };
                    options.RelativePath = uri;
                });
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/pagedAndFilteredProductSnapshots?aggregateId={aggregateId}&category={category}&sortColumn={sortColumn}&pageNumber={pageNumber}&pageSize={pageSize}";
            Console.WriteLine($"BLAZOR DEV CLIENT HTTP CLIENT CALL URI: {uri}");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/productSnapshots?aggregateId={aggregateId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/productSnapshot/{aggregateId}?minVersion={minVersion}&maxVersion={maxVersion}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/eventRecords?aggregateId={aggregateId}&correlationId={correlationId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";
            _logger.LogInformation("GET EVENT RECORDS URI: {uri}", uri);

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/outboxRecords?aggregateId={aggregateId}&correlationId={correlationId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/snapshotRecords?aggregateId={aggregateId}&correlationId={correlationId}&minVersion={minVersion}&maxVersion={maxVersion}&pageNumber={pageNumber}&pageSize={pageSize}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/throwExceptionForTesting";
            var stringContent = new StringContent(JsonSerializer.Serialize(throwExceptionDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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
                string error = await GetErrorMessageAsync(response);
                return (false, error);
            }
        }

        public async Task<(bool IsSuccess, string? Value, string? ErrorMessage)> GetCloudAmqpSettingsTestingDummyValueAsync(CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/getCloudAmqpSettingsTestingDummyValue";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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

        public async Task<(bool IsSuccess, string? ErrorMessage)> PurgeDataAsync(PurgeDataDTO purgeDataDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/purgeData";

            var stringContent = new StringContent(JsonSerializer.Serialize(purgeDataDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
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

            if (response.IsSuccessStatusCode) return (true, null);
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> DeleteProductByIdAsync(Guid aggregateId, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsWriteApiService_DevTestsPath}/permanentlyDeleteProduct?aggregateId={aggregateId}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.ProductsWriteApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "DELETE";
                    options.ExtraHeaderParameters = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    };
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
