using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Carts;
using Consumer.Blazor.Client.DTOs.Orders;

namespace Consumer.Blazor.Client.Mappers
{
    public class OrderMapper : IOrderMapper
    {
        public AddOrderDTO MapCartToAddOrderDTO(ShoppingCartDTO shoppingCartDTO)
        {
            return new AddOrderDTO
            {
                Items = shoppingCartDTO.Items.Select(item => new AddOrderItemDTO
                {
                    ProductId = item.ProductId,
                    AggregateId = item.AggregateId,
                    Category = item.Category,
                    Name = item.Name,
                    Currency = item.Currency,
                    Price = item.Price,
                    UOM = item.UOM,
                    Quantity = item.Quantity,
                }).ToList(),
                ShippingAddress = null,
                BillingAddress = null
            };
        }

        public List<AddOrderItemDTO> MapCartItemDTOsToAddOrderItemDTOs(IEnumerable<ShoppingCartItemDTO> cartItemDTOs)
        {
            List<AddOrderItemDTO> orderItemDTOs = new List<AddOrderItemDTO>();
            if (cartItemDTOs is null) return orderItemDTOs;
            foreach (var item in cartItemDTOs)
            {
                AddOrderItemDTO orderItemDTO = new AddOrderItemDTO()
                {
                    ProductId = item.ProductId,
                    Category = item.Category,
                    Name = item.Name,
                    Currency = item.Currency,
                    Price = item.Price,
                    UOM = item.UOM,
                    Quantity = item.Quantity
                };
                orderItemDTOs.Add(orderItemDTO);
            }
            return orderItemDTOs;
        }
    }
}
