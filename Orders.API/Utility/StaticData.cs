namespace Orders.API.Utility
{
    public class StaticData
    {
        // ORDER STATUS 
        public const string OrderStatus_Placed = "Placed";
        public const string OrderStatus_Updated = "Updated";
        public const string OrderStatus_Complete = "Complete";

        // INTERNAL ACCOUNT HTTP CLIENT (API KEY AUTH)
        public const string InternalAccounts_HttpClient_Name = "InternalAccountsHttpClient";
        public const string InternalAccounts_HttpClient_Local_BaseUrl = "https://localhost:7033";
        public const string InternalAccounts_HttpClient_AccountsPath = "/api/internalAccounts";

        // API KEY AUTH
        public const string OrdersToAccountsApiKeyHeaderName = "X-OrdersToAccounts-API-Key";
        public const string OrdersToAccountsApiKeyName = "OrdersToAccountsApiKey";

    }
}
