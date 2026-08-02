using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Accounts;
using Consumer.Blazor.Client.Utility;
using Microsoft.Identity.Abstractions;
using System.Text;
using System.Text.Json;

namespace Consumer.Blazor.DownstreamApiServices
{
    public class AccountsApiService : IAccountsHttpService
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<AccountsApiService> _logger;

        public AccountsApiService(IDownstreamApi downstreamApi, ILogger<AccountsApiService> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AccountIsEstablished()
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
                    string jsonAccount = JsonSerializer.Serialize(accountDTO);
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
}
