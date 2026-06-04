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
        }
    }
}
