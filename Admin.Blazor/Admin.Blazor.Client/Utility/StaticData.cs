namespace Admin.Blazor.Client.Utility
{
    public class StaticData
    {
        // NOTE: This client accesses API Resources through the YARP Reverse Proxy.
        /// WASM API Base Address
        public const string WasmClient_LocalApiBaseAddress = "https://localhost:7088";
        public const string WasmClient_AzureApiBaseAddress = "https://pawnshopadmin-cfd2asdcc2eceeac.centralus-01.azurewebsites.net";

        // YARP Reverse Proxy
        public const string ProductsApiServices_LocalYarpProxyBaseURL = "https://localhost:7245";

        // DownstreamApi Services
        public const string ProductsWriteApiService_ServiceName = "MikesPawnshopProductsWriteAPI";
        // public const string ProductsWriteApiService_LocalBaseURL = "https://localhost:7213";          // local direct 
        public const string ProductsWriteApiService_LocalBaseURL = "https://localhost:7245";             // with YARP
        public const string ProductsWriteApiService_ProductsPath = "/api/productsManagement";
        public const string ProductsWriteApiService_DevTestsPath = "/dev/productsManagement";
        public const string ProductsWriteApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string ProductsReadApiService_ServiceName = "MikesPawnshopProductsReadAPI";
        // public const string ProductsReadApiService_LocalBaseURL = "https://localhost:7101";          // local direct
        public const string ProductsReadApiService_LocalBaseURL = "https://localhost:7245";             // local YARP
        public const string ProductsReadApiService_ProductsPath = "/api/products";
        public const string ProductsReadApiService_DevTestsPath = "/dev/products";
        public const string ProductsReadApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string CartsApiService_ServiceName = "MikesPawnshopCartsAPI";
        // public const string CartsApiService_LocalBaseURL = "https://localhost:7184";             // local direct
        public const string CartsApiService_LocalBaseURL = "https://localhost:7245";                // local YARP
        public const string CartsApiService_CartsPath = "/api/carts";
        public const string CartsApiService_DevTestsPath = "/api/carts/devTests";
        public const string CartsApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string AccountsApiService_ServiceName = "MikesPawnshopAccountsAPI";
        // public const string AccountsApiService_LocalBaseURL = "https://localhost:7033";          // local direct
        public const string AccountsApiService_LocalBaseURL = "https://localhost:7245";             // local YARP
        public const string AccountsApiService_AccountsPath = "/api/accounts";
        public const string AccountsApiService_DevTestsPath = "/api/accounts/devTests";
        public const string AccountsApiService_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string OrdersApiService_ServiceName = "MikesPawnshopOrdersAPI";
        // public const string OrdersApiService_LocalBaseURL = "https://localhost:7019";          // local direct
        public const string OrdersApiService_LocalBaseURL = "https://localhost:7245";             // local YARP
        public const string OrdersApiService_OrdersPath = "/api/orders";
        public const string OrdersApiService_DevTestsPath = "/api/orders/devTests";
        public const string OrdersApiService_GetApiUserInfoSubpath = "/getApiUserInfo";


        // Standard Http Clients
        public const string AzureServicesHttpClient_ClientName = "AzureServiceStatusHttpClient";

        public const string ProductsWriteHttpClient_ClientName = "ProductsWriteHttpClient";
        // public const string ProductsWriteHttpClient_BaseURL = "https://localhost:7213";              // local direct
        public const string ProductsWriteHttpClient_BaseURL = "https://localhost:7245";                 // local YARP
        public const string ProductsWriteHttpClient_ProductsPath = "/api/productsManagement";
        public const string ProductsWriteHttpClient_DevTestsPath = "/dev/productsManagement";
        public const string ProductsWriteHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string ProductsReadHttpClient_ClientName = "ProductsReadHttpClient";
        // public const string ProductsReadHttpClient_BaseURL = "https://localhost:7101";               // local direct
        public const string ProductsReadHttpClient_BaseURL = "https://localhost:7245";                  // local YARP
        public const string ProductsReadHttpClient_ProductsPath = "/api/products";
        public const string ProductsReadHttpClient_DevTestsPath = "/api/products/devTests";
        public const string ProductsReadHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string CartsHttpClient_ClientName = "CartsHttpClient";
        // public const string CartsHttpClient_BaseURL = "https://localhost:7184";                      // local direct
        public const string CartsHttpClient_BaseURL = "https://localhost:7245";                         // local YARP
        public const string CartsHttpClient_CartsPath = "/api/carts";
        public const string CartsHttpClient_DevTestsPath = "/api/carts/devTests";
        public const string CartsHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string AccountsHttpClient_ClientName = "AccountsHttpClient";
        // public const string AccountsHttpClient_BaseURL = "https://localhost:7033";                   // local direct
        public const string AccountsHttpClient_BaseURL = "https://localhost:7245";                      // local YARP
        public const string AccountsHttpClient_AccountsPath = "/api/accounts";
        public const string AccountsHttpClient_DevTestsPath = "/api/accounts/devTests";
        public const string AccountsHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";

        public const string OrdersHttpClient_ClientName = "OrdersHttpClient";
        // public const string OrdersHttpClient_BaseURL = "https://localhost:7019";                     // local direct
        public const string OrdersHttpClient_BaseURL = "https://localhost:7245";                        // local YARP
        public const string OrdersHttpClient_OrdersPath = "/api/orders";
        public const string OrdersHttpClient_DevTestsPath = "/api/orders/devTests";
        public const string OrdersHttpClient_GetApiUserInfoSubpath = "/getApiUserInfo";
    }
}
