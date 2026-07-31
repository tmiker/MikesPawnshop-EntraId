using Carts.API.DTOs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Carts.API.Domain.Models
{
    public class ShoppingCart
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; private set; }
        public string? ShoppingCartId { get; private set; }     
        public string? OwnerId { get; private set; }
        public int CreditLimit { get; private set; }
        public List<ShoppingCartItem> Items { get; private set; } = new List<ShoppingCartItem>();

        private ShoppingCart() { }

        public ShoppingCart(string ownerId, int creditLimit)
        {
            ShoppingCartId = Guid.NewGuid().ToString();
            OwnerId = ownerId;
            CreditLimit = creditLimit;
            Items = new List<ShoppingCartItem>();
        }

        public void AddCartItem(ShoppingCartItem item)
        {
            ShoppingCartItem? existingItem = Items.FirstOrDefault(i => i.AggregateId == item.AggregateId);
            if (existingItem is null)
            {
                int maxLineNumber = Items.Count > 0 ? Items.Max(i => i.LineNumber) : 0;
                item.SetLineNumber(maxLineNumber + 1);
                Items.Add(item);
            }
            else
            {
                existingItem.UpdateItemQuantity(item.Quantity);
            }
        }

        public void UpdateCartItemQuantity(string aggregateId, double amount)
        {
            ShoppingCartItem? existingItem = Items.FirstOrDefault(i => i.AggregateId == aggregateId);
            if (existingItem is not null)
            {
                existingItem.UpdateItemQuantity(amount);
                if (existingItem.Quantity <= 0)
                {
                    Items.Remove(existingItem);
                }
            }
        }

        public void RemoveCartItem(string aggregateId)
        {
            ShoppingCartItem? existingItem = Items.FirstOrDefault(i => i.AggregateId == aggregateId);
            if (existingItem is not null)
            {
                Items.Remove(existingItem);
            }
        }

        public ShoppingCartDTO ToShoppingCartDTO()
        {
            List<ShoppingCartItemDTO> itemDTOs = new List<ShoppingCartItemDTO>();
            Items.ForEach(i => itemDTOs.Add(i.ToShoppingCartItemDTO()));
            return new ShoppingCartDTO()
            {
                Id = Id,
                ShoppingCartId = ShoppingCartId,
                CreditLimit = CreditLimit,
                Items = itemDTOs
            };
        }
    }
}
