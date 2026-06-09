using Carts.API.DTOs;

namespace Carts.API.Abstractions
{
    public interface ICartService
    {
        Task<(bool IsSuccess, int CartCount, string? ErrorMessage)> GetCartCountAsync();
        Task<bool> CreateCartAsync(string ownerId);
        Task<ShoppingCartDTO> GetCartAsync(string ownerId);
        Task<bool> RemoveCartAsync(string ownerId);
        Task<(bool IsSuccess, int CartItemQuantity, string? ErrorMessage)> AddNewCartItemAsync(string ownerId, AddShoppingCartItemDTO addShoppingCartItemDTO);
        Task<bool> UpdateCartItemQuantityAsync(string ownerId, string aggregateId, double amount);
        Task<bool> RemoveCartItemAsync(string ownerId, string aggregateId);
    }
}
