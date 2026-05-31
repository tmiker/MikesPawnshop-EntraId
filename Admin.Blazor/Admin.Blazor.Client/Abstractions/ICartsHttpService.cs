using Admin.Blazor.Client.DTOs.Carts;
using Admin.Blazor.Client.DTOs.Health;

namespace Admin.Blazor.Client.Abstractions
{
    public interface ICartsHttpService
    {
        Task<(bool IsSuccess, HealthCheckResultDTO? HealthCheckResultDTO, string? ErrorMessage)> CheckHealthAsync();
        Task<(bool IsSuccess, int CartItemCount, string? ErrorMessage)> AddNewCartItemAsync(AddShoppingCartItemDTO addShoppingCartItemDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> UpdateProductQuantityAsync(string aggregateId, int amount);
        Task<(bool IsSuccess, string? ErrorMessage)> RemoveCartItemAsync(string aggregateId);
        Task<(bool IsSuccess, ShoppingCartDTO? ShoppingCart, string? ErrorMessage)> GetShoppingCartAsync();
        Task<(bool IsSuccess, string? ErrorMessage)> RemoveShoppingCartAsync();
    }
}
