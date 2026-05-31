using Consumer.Blazor.Client.DTOs.Carts;

namespace Consumer.Blazor.Client.Abstractions
{
    public interface ICartsHttpService
    {
        Task<(bool IsSuccess, int CartItemCount, string? ErrorMessage)> AddNewCartItemAsync(AddShoppingCartItemDTO addShoppingCartItemDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> UpdateProductQuantityAsync(string aggregateId, int amount);
        Task<(bool IsSuccess, string? ErrorMessage)> RemoveCartItemAsync(string aggregateId);
        Task<(bool IsSuccess, ShoppingCartDTO? ShoppingCart, string? ErrorMessage)> GetShoppingCartAsync();
        Task<(bool IsSuccess, string? ErrorMessage)> RemoveShoppingCartAsync();
    }
}
