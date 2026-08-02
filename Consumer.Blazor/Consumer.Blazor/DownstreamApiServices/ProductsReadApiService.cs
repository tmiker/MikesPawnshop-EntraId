using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Products.Test;
using Consumer.Blazor.Client.ErrorHandling;
using Consumer.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text;
using System.Text.Json;

namespace Consumer.Blazor.DownstreamApiServices
{
    public class ProductsReadApiService : IProductsReadHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<ProductsReadApiService> _logger;

        public ProductsReadApiService(IDownstreamApi downstreamApi, ILogger<ProductsReadApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        // Dev Tests
        public async Task<(bool IsSuccess, string? ErrorMessage)> ThrowExceptionForTestingAsync(ThrowExceptionDTO throwExceptionDTO, CancellationToken cancellationToken)
        {
            string uri = $"{StaticData.ProductsReadApiService_DevTestsPath}/throwExceptionForTesting";
            var stringContent = new StringContent(JsonSerializer.Serialize(throwExceptionDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
               serviceName: StaticData.ProductsReadApiService_ServiceName,
               downstreamApiOptionsOverride: options =>
               {
                   options.HttpMethod = "POST";
                   options.RelativePath = uri;
                   options.ExtraHeaderParameters = new Dictionary<string, string>
                   {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                   };
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

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
            {
                try
                {
                    CustomProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<CustomProblemDetails>();
                    return problemDetails?.ToString()!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    var result = await response.Content.ReadAsStringAsync();
                    return result;
                }
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
