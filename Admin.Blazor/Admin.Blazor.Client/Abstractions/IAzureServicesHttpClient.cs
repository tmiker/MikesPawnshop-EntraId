namespace Admin.Blazor.Client.Abstractions
{
    public interface IAzureServicesHttpClient
    {
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckAccountsApiAsync();
        Task<(bool IsSuccess, string? Count, string? ErrorMessage)> CheckAccountsMongoAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckCartsApiAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckOrdersApiAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsReadApiAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsWriteApiAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsReadSqlAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckProductsWriteSqlAsync();
        Task<(bool IsSuccess, string? Result, string? ErrorMessage)> CheckYarpProxyAsync();
    }
}
