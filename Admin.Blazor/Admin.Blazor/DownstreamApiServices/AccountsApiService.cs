using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Accounts;
using Admin.Blazor.Client.DTOs.Health;
using Admin.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text;
using System.Text.Json;

namespace Admin.Blazor.DownstreamApiServices
{
    public class AccountsApiService : IAccountsHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<AccountsApiService> _logger;
        private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public AccountsApiService(IDownstreamApi downstreamApi, ILogger<AccountsApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync()
        {
            string uri = $"{StaticData.AccountsApiService_AccountsPath}/healthClient";

            try
            {
                var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.AccountsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });
            
                response.EnsureSuccessStatusCode();
                var resultDTO = await response.Content.ReadFromJsonAsync<HealthCheckResultDTO>(_jsonSerializerOptions);
                if (resultDTO is not null)
                {
                    _logger.LogInformation("AccountsApiService CheckHealthAsync() at path '{uri}' Result: \n{resultDTO}", uri, JsonSerializer.Serialize(resultDTO));
                    return (true, resultDTO, null);
                }
                else return (false, null, "Health check result DTO is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError("AccountsApiService CheckHealthAsync() at path '{uri}' Exception: {ex.Message}", uri, ex.Message);
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AccountIsEstablishedAsync()
        {
            string uri = $"{StaticData.AccountsApiService_AccountsPath}/accountEstablished";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.AccountsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            else
            {
                return (false, "An account was not found");
            }
        }

        public async Task<(bool IsSuccess, AccountDTO? Account, string? ErrorMessage)> GetAccountAsync()
        {
            string uri = $"{StaticData.AccountsApiService_AccountsPath}";

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.AccountsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "GET";
                    options.RelativePath = uri;
                });

            if (response.IsSuccessStatusCode)
            {
                AccountDTO? accountDTO = await response.Content.ReadFromJsonAsync<AccountDTO>();
                if (accountDTO is not null)
                {
                    _logger.LogInformation("Account retrieved successfully for user first name {accountDTO.FirstName}", accountDTO.FirstName);
                }
                else
                {
                    _logger.LogWarning("Unable to retrieve the Account data for the current user.");
                }
                return (true, accountDTO, null);
            }
            else
            {
                string errorMessage = await GetErrorMessageAsync(response);
                return (false, null, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> CreateAccountAsync(AddAccountDTO addAccountDTO)
        {
            string uri = $"{StaticData.AccountsApiService_AccountsPath}";
            var stringContent = new StringContent(JsonSerializer.Serialize(addAccountDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.AccountsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "POST";
                    options.RelativePath = uri;
                },
                user: null,
                content: stringContent);

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

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddAddressAsync(AddAddressDTO addAddressDTO)
        {
            string uri = $"{StaticData.AccountsApiService_AccountsPath}/addAddress";
            var stringContent = new StringContent(JsonSerializer.Serialize(addAddressDTO), Encoding.UTF8, "application/json");

            var response = await _downstreamApi.CallApiForUserAsync(
                serviceName: StaticData.AccountsApiService_ServiceName,
                downstreamApiOptionsOverride: options =>
                {
                    options.HttpMethod = "PUT";
                    options.RelativePath = uri;
                },
                user: null,
                content: stringContent);

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
