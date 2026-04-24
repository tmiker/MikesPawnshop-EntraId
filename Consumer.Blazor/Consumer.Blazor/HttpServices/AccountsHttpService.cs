using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Accounts;
using Consumer.Blazor.Client.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Consumer.Blazor.HttpServices
{
    public class AccountsHttpService : IAccountsHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountsHttpService> _logger;

        public AccountsHttpService(IHttpClientFactory httpClientFactory, ILogger<AccountsHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> AccountIsEstablished()
        {
            string uri = $"{StaticData.AccountsHttpClient_AccountsPath}/accountEstablished";
            var client = _httpClientFactory.CreateClient(StaticData.AccountsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request);

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
            string uri = $"{StaticData.AccountsHttpClient_AccountsPath}";
            var client = _httpClientFactory.CreateClient(StaticData.AccountsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                AccountDTO? accountDTO = await response.Content.ReadFromJsonAsync<AccountDTO>();
                if (accountDTO is not null)
                {
                    string jsonAccount = JsonSerializer.Serialize(accountDTO);
                    Console.WriteLine($"\n************\nAccountsHttpService GetAccountAsync() result: \n{jsonAccount}\n************\n");
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
            string uri = $"{StaticData.AccountsHttpClient_AccountsPath}";
            var client = _httpClientFactory.CreateClient(StaticData.AccountsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(addAccountDTO), Encoding.UTF8, "application/json");
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

        public async Task<(bool IsSuccess, string? ErrorMessage)> AddAddressAsync(AddAddressDTO addAddressDTO)
        {
            string uri = $"{StaticData.AccountsHttpClient_AccountsPath}/addAddress";
            var client = _httpClientFactory.CreateClient(StaticData.AccountsHttpClient_ClientName);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
            request.Content = new StringContent(JsonSerializer.Serialize(addAddressDTO), Encoding.UTF8, "application/json");
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
