namespace Admin.Blazor.Client.Utility
{
    public class StaticData
    {
        // NOTE: This client accesses API Resources through the YARP Reverse Proxy.

        /// WASM API Base Address
        public const string WasmClient_LocalApiBaseAddress = "https://localhost:7088";

        // DownstreamApi Services
        public const string ProductsWriteApiService_ServiceName = "ProductsWriteApiService";
        // public const string ProductsWriteApiService_BaseURL = "https://localhost:7213";          // without YARP
        public const string ProductsWriteApiService_LocalBaseURL = "https://localhost:7245";         // local YARP
        // public const string ProductsWriteApiService_AzureBaseURL = "pending";                       // Azure deployed YARP
        public const string ProductsWriteApiService_ProductsPath = "/api/productsManagement";
        public const string ProductsWriteApiService_DevTestsPath = "/api/productsManagement/devTests";
        public const string ProductsWriteApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string ProductsReadApiService_ServiceName = "ProductsReadApiService";
        // public const string ProductsReadApiService_BaseURL = "https://localhost:7101";
        public const string ProductsReadApiService_LocalBaseURL = "https://localhost:7245";          // local YARP
        // public const string ProductsReadApiService_AzureBaseURL = "pending";                        // Azure deployed YARP
        public const string ProductsReadApiService_ProductsPath = "/api/products";
        public const string ProductsReadApiService_DevTestsPath = "/api/products/devTests";
        public const string ProductsReadApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string CartsApiService_ServiceName = "CartsApiService";
        // public const string CartsApiService_BaseURL = "https://localhost:7184";
        public const string CartsApiService_LocalBaseURL = "https://localhost:7245";            // local YARP
        // public const string CartsApiService_AzureBaseURL = "pending";                        // Azure deployed YARP
        public const string CartsApiService_CartsPath = "/api/carts";
        public const string CartsApiService_DevTestsPath = "/api/carts/devTests";
        public const string CartsApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string AccountsApiService_ServiceName = "AccountsApiService";
        // public const string AccountsApiService_BaseURL = "https://localhost:7033";
        public const string AccountsApiService_LocalBaseURL = "https://localhost:7245";          // local YARP
        // public const string AccountsApiService_AzureBaseURL = "pending";                        // Azure deployed YARP
        public const string AccountsApiService_AccountsPath = "/api/accounts";
        public const string AccountsApiService_DevTestsPath = "/api/accounts/devTests";
        public const string AccountsApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string OrdersApiService_ServiceName = "OrdersApiService";
        // public const string OrdersApiService_BaseURL = "https://localhost:7019";
        public const string OrdersApiService_LocalBaseURL = "https://localhost:7245";          // local YARP
        // public const string OrdersApiService_AzureBaseURL = "pending";                          // Azure deployed YARP
        public const string OrdersApiService_OrdersPath = "/api/orders";
        public const string OrdersApiService_DevTestsPath = "/api/orders/devTests";
        public const string OrdersApiService_GetApiUserInfoSubpath = "/getApiUserInfo";




        // Http Services
        public const string ProductsWriteHttpClient_ClientName = "ProductsWriteHttpClient";
        // public const string ProductsWriteHttpClient_BaseURL = "https://localhost:7213";      
        public const string ProductsWriteHttpClient_BaseURL = "https://localhost:7245";         // YARP
        public const string ProductsWriteHttpClient_ProductsPath = "/api/productsManagement";
        public const string ProductsWriteHttpClient_DevTestsPath = "/api/productsManagement/devTests";
        public const string ProductsWriteHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string ProductsReadHttpClient_ClientName = "ProductsReadHttpClient";
        // public const string ProductsReadHttpClient_BaseURL = "https://localhost:7101";
        public const string ProductsReadHttpClient_BaseURL = "https://localhost:7245";          // YARP
        public const string ProductsReadHttpClient_ProductsPath = "/api/products";
        public const string ProductsReadHttpClient_DevTestsPath = "/api/products/devTests";
        public const string ProductsReadHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string CartsHttpClient_ClientName = "CartsHttpClient";
        // public const string CartsHttpClient_BaseURL = "https://localhost:7184";
        public const string CartsHttpClient_BaseURL = "https://localhost:7245";          // YARP
        public const string CartsHttpClient_CartsPath = "/api/carts";
        public const string CartsHttpClient_DevTestsPath = "/api/carts/devTests";
        public const string CartsHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string AccountsHttpClient_ClientName = "AccountsHttpClient";
        // public const string AccountsHttpClient_BaseURL = "https://localhost:7033";
        public const string AccountsHttpClient_BaseURL = "https://localhost:7245";          // YARP
        public const string AccountsHttpClient_AccountsPath = "/api/accounts";
        public const string AccountsHttpClient_DevTestsPath = "/api/accounts/devTests";
        public const string AccountsHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string OrdersHttpClient_ClientName = "OrdersHttpClient";
        // public const string OrdersHttpClient_BaseURL = "https://localhost:7019";
        public const string OrdersHttpClient_BaseURL = "https://localhost:7245";          // YARP
        public const string OrdersHttpClient_OrdersPath = "/api/orders";
        public const string OrdersHttpClient_DevTestsPath = "/api/orders/devTests";
        public const string OrdersHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";
    }
}
