using Orders.API.Abstractions;
using Orders.API.DTOs;
using Orders.API.Utility;
using System.Text;
using System.Text.Json;

namespace Orders.API.Services
{
    public class InternalAccountsHttpService : IInternalAccountsHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<InternalAccountsHttpService> _logger;

        public InternalAccountsHttpService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<InternalAccountsHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, KeyContainerResponseDTO? KeyContainerResponse, string? ErrorMessage)> GetKeyContainerDataForAccountsAsync()
        {
            // get account api key to add to header when making internal api calls
            string? apiKey = _config.GetValue<string>(StaticData.OrdersToAccountsApiKeyName);
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, null, "Error: missing credentials");

            // 1.GET PUBLIC KEY IN ORDER TO ENCRYPT OWNERID(CREATES A CONTAINER WITH KEYS USING CONTAINER NAME IF DOES NOT ALREADY EXIST)
            string keyUri = $"{StaticData.InternalAccounts_HttpClient_AccountsPath}/getPublicKeyForSpecifiedContainer";
            var client = _httpClientFactory.CreateClient(StaticData.InternalAccounts_HttpClient_Name);

            // MOVED CONTAINER NAMING TO INTERNAL SERVICE TO ENSURE UNIQUENESS !!! Create request dto with unique container name (containers deleted at end of each request reply cycle)

            HttpRequestMessage keyRequest = new HttpRequestMessage(HttpMethod.Get, keyUri);

            keyRequest.Headers.Add(StaticData.OrdersToAccountsApiKeyHeaderName, apiKey);
            // keyRequest.Content = new StringContent(JsonSerializer.Serialize(keyContainerRequestDTO), Encoding.UTF8, "application/json");
            using (HttpResponseMessage keyResponse = await client.SendAsync(keyRequest))
            {
                if (keyResponse.IsSuccessStatusCode)
                {
                    KeyContainerResponseDTO? keyContainerResponse = await keyResponse.Content.ReadFromJsonAsync<KeyContainerResponseDTO>();
                    return (true, keyContainerResponse, null);
                }
                else
                {
                    string errorMessage = $"Failed to retrieve a key container response at URI: {keyRequest.RequestUri}. Status code: {keyResponse.StatusCode}";        // ***
                    _logger.LogError(errorMessage);
                    return (false, null, errorMessage);
                }
            }
        }

        //public async Task<(bool IsSuccess, KeyContainerResponseDTO? KeyContainerResponse, string? ErrorMessage)> GetKeyContainerDataForAccountsAsync()
        //{
        //    string uri = $"{StaticData.InternalAccounts_HttpClient_AccountsPath}/getPublicKeyForSpecifiedContainer";
        //    var client = _httpClientFactory.CreateClient(StaticData.InternalAccounts_HttpClient_Name);

        //    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

        //    using (HttpResponseMessage response = await client.SendAsync(request))
        //    {
        //        if (response.IsSuccessStatusCode)
        //        {
        //            KeyContainerResponseDTO? keyContainerResponseDTO = await response.Content.ReadFromJsonAsync<KeyContainerResponseDTO>();
        //            return (true, keyContainerResponseDTO, null);
        //        }
        //        else
        //        {
        //            string errorMessage = $"Failed to retrieve a key container response. Status code: {response.StatusCode}";
        //            _logger.LogError(errorMessage);
        //            return (false, null, errorMessage);
        //        }
        //    }
        //}

        public async Task<AccountStatusResponseDTO> GetUserAccountStatusAsync(AccountStatusRequestDTO accountStatusRequestDTO, CancellationToken? cancellationToken = null)
        {
            // get account api key to add to header when making internal api calls
            string? apiKey = _config.GetValue<string>(StaticData.OrdersToAccountsApiKeyName);
            if (string.IsNullOrWhiteSpace(apiKey)) return new AccountStatusResponseDTO() { IsSuccess = false, Errors = new List<string>() { "Error: missing required credentials" } };

            // GET DATA FROM PRIVATE API BY PROVIDING ENCRYPTED OWNERID
            string dataUri = $"{StaticData.InternalAccounts_HttpClient_AccountsPath}/status";
            var client = _httpClientFactory.CreateClient(StaticData.InternalAccounts_HttpClient_Name);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, dataUri);
            request.Headers.Add(StaticData.OrdersToAccountsApiKeyHeaderName, apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(accountStatusRequestDTO), Encoding.UTF8, "application/json");
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    AccountStatusResponseDTO? accountStatusResponse = await response.Content.ReadFromJsonAsync<AccountStatusResponseDTO>();
                    if (accountStatusResponse is null) return new AccountStatusResponseDTO() { IsSuccess = false, Errors = new List<string>() { "Error: the account status response was null" } };
                    return accountStatusResponse;
                }
                else
                {
                    string errorMessage = $"Failed to retrieve a user account status at URI: {request.RequestUri}. Status code: {response.StatusCode}";
                    _logger.LogError(errorMessage);
                    AccountStatusResponseDTO responseDTO = new AccountStatusResponseDTO()
                    {
                        IsSuccess = false,
                        Status = null,
                        Errors = new List<string>() { errorMessage }
                    };
                    return responseDTO;
                }
            }

            //string uri = $"{StaticData.InternalAccounts_HttpClient_AccountsPath}";
            //var client = _httpClientFactory.CreateClient(StaticData.InternalAccounts_HttpClient_Name);

            //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            //using (HttpResponseMessage response = await client.SendAsync(request))
            //{
            //    if (response.IsSuccessStatusCode)
            //    {
            //        AccountStatusResponseDTO? accountStatusDTO = await response.Content.ReadFromJsonAsync<AccountStatusResponseDTO>();
            //        if (accountStatusDTO is not null) return accountStatusDTO;
            //        else return new AccountStatusResponseDTO()
            //        {
            //            IsSuccess = false,
            //            Status = null,
            //            Errors = new List<string>() { "Failed to deserialize user account status response." }
            //        };
            //    }
            //    else
            //    {
            //        string errorMessage = $"Failed to retrieve user account status response. Status code: {response.StatusCode}";
            //        _logger.LogError(errorMessage);
            //        AccountStatusResponseDTO responseDTO = new AccountStatusResponseDTO()
            //        {
            //            IsSuccess = false,
            //            Status = null,
            //            Errors = new List<string>() { errorMessage }
            //        };
            //        return responseDTO;
            //    }
            //}
        }
    }
}
