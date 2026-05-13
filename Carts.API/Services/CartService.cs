using Carts.API.Abstractions;
using Carts.API.Domain.Models;
using Carts.API.DTOs;
using MongoDB.Driver;

namespace Carts.API.Services
{
    public class CartService : ICartService
    {
        private readonly IConfiguration _config;
        private readonly IMongoCollection<ShoppingCart> _carts;
        private readonly ILogger<CartService> _logger;
        private readonly int _baseCreditLimit = 5000;

        public CartService(IConfiguration config, ILogger<CartService> logger)
        {
            _config = config;
            string? environment = _config["ASPNETCORE_ENVIRONMENT"];
            var client = environment == "Development" ? new MongoClient(_config["LOCAL_MONGO_CONNECTION"]) :
                new MongoClient(_config["AZURE_MONGO_CONNECTION"]);
            var database = client.GetDatabase(_config["MONGO_DATABASE"]);
            _carts = database.GetCollection<ShoppingCart>(_config["MONGO_CART_COLLECTION"]);
            _logger = logger;
        }

        private async Task<ShoppingCart> EnsureCartExistsAsync(string ownerId)
        {
            ShoppingCart? cart = await _carts.Find(c => c.OwnerId == ownerId).FirstOrDefaultAsync();
            if (cart is null)
            {
                cart = new ShoppingCart(ownerId, _baseCreditLimit);
                await _carts.InsertOneAsync(cart);
            }
            return cart;
        }

        public async Task<bool> CreateCartAsync(string ownerId)
        {
            bool exists = await _carts.Find(c => c.OwnerId == ownerId).AnyAsync();
            if (!exists)
            {
                ShoppingCart cart = new ShoppingCart(ownerId, _baseCreditLimit);
                await _carts.InsertOneAsync(cart);
            }
            return true;
        }

        public async Task<ShoppingCartDTO> GetCartAsync(string ownerId)
        {
            ShoppingCart? cart = await _carts.Find(c => c.OwnerId == ownerId).FirstOrDefaultAsync();
            if (cart is null)
            {
                cart = new ShoppingCart(ownerId, _baseCreditLimit);
                await _carts.InsertOneAsync(cart);
            }
            ShoppingCartDTO cartDTO = cart.ToShoppingCartDTO();
            return cartDTO;
        }

        public async Task<bool> RemoveCartAsync(string ownerId)
        {
            var result = await _carts.DeleteOneAsync(c => c.OwnerId == ownerId);
            if (result.DeletedCount > 0) return true;
            return false;
        }

        public async Task<(bool IsSuccess, int CartItemQuantity, string? ErrorMessage)> AddNewCartItemAsync(string ownerId, AddShoppingCartItemDTO addShoppingCartItemDTO)
        {
            ShoppingCart cart = await EnsureCartExistsAsync(ownerId);

            cart.AddCartItem(new ShoppingCartItem(addShoppingCartItemDTO, cart.ShoppingCartId!));

            var result = await _carts.ReplaceOneAsync(c => c.ShoppingCartId == cart.ShoppingCartId, cart);

            return (true, cart.Items.Count, null);
        }

        public async Task<bool> UpdateCartItemQuantityAsync(string ownerId, string aggregateId, double amount)
        {
            ShoppingCart? cart = await _carts.Find(c => c.OwnerId == ownerId).FirstOrDefaultAsync();

            if (cart is not null)
            {
                cart.UpdateCartItemQuantity(aggregateId, amount);
                var result = await _carts.ReplaceOneAsync(c => c.ShoppingCartId == cart.ShoppingCartId, cart);
                return result.ModifiedCount > 0;
            }
            else
            {
                _logger.LogWarning("A cart with OwnerId: {OwnerId} was not found", ownerId);
                return false;
            }
        }

        public async Task<bool> RemoveCartItemAsync(string ownerId, string aggregateId)
        {
            ShoppingCart? cart = await _carts.Find(c => c.OwnerId == ownerId).FirstOrDefaultAsync();

            if (cart is not null)
            {
                cart.RemoveCartItem(aggregateId);
                var result = await _carts.ReplaceOneAsync(c => c.ShoppingCartId == cart.ShoppingCartId, cart);
                return result.ModifiedCount > 0;
            }
            else
            {
                _logger.LogWarning("A cart with OwnerId: {OwnerId} was not found", ownerId);
                return false;
            }
        }
    }
}
