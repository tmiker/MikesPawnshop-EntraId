namespace Orders.API.Utility
{
    public class StaticData
    {
        // ORDER STATUS 
        public const string OrderStatus_Placed = "Placed";
        public const string OrderStatus_Updated = "Updated";
        public const string OrderStatus_Complete = "Complete";

        // ACCOUNT HTTP CLIENT 
        public const string InternalAccounts_HttpClient_Name = "InternalAccountsHttpClient";
        public const string InternalAccounts_HttpClient_BaseUrl = "https://localhost:7033";
        public const string InternalAccounts_HttpClient_AccountsPath = "/api/accounts";         
        // [HttpGet] [Authorize] public async Task<ActionResult<AccountDTO>> GetByOwnerId()

        // ACCOUNT STATUS OPTIONS
        public const string AccountStatus_Active = "Active";
        public const string AccountStatus_Hold = "Hold";
    }
}
