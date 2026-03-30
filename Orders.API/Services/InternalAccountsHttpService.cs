using Orders.API.Abstractions;
using Orders.API.DTOs;
using Orders.API.Utility;

namespace Orders.API.Services
{
    public class InternalAccountsHttpService : IInternalAccountsHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InternalAccountsHttpService> _logger;

        public InternalAccountsHttpService(IHttpClientFactory httpClientFactory, ILogger<InternalAccountsHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, AccountDTO? AccountDTO, string? ErrorMessage)> GetUserAccountDataAsync(CancellationToken? cancellationToken = null)
        {
            // Calls Accounts.API to get the user account data - no arguments needed as the user is identified by the access token 
            // Accounts.API endpoint:  // [HttpGet] [Authorize] public async Task<ActionResult<AccountDTO>> GetByOwnerId()

            string uri = $"{StaticData.InternalAccounts_HttpClient_AccountsPath}/internalStatusCheck";
            var client = _httpClientFactory.CreateClient(StaticData.InternalAccounts_HttpClient_Name);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    AccountDTO? accountDTO = await response.Content.ReadFromJsonAsync<AccountDTO>();
                    return (true, accountDTO, null);
                }
                else
                {
                    string errorMessage = $"Failed to retrieve user account data. Status code: {response.StatusCode}";
                    _logger.LogError(errorMessage);
                    return (false, null, errorMessage);
                }
            }

        }

    }
}
