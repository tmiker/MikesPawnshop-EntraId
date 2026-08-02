using Consumer.Blazor.Client.DTOs.Accounts;
using System.Text.Json.Serialization;

namespace Consumer.Blazor.Client.DTOs.Orders
{
    public class AddOrderDTO
    {
        public List<AddOrderItemDTO> Items { get; set; } = new List<AddOrderItemDTO>();
        public AddressDTO? ShippingAddress { get; set; }
        public AddressDTO? BillingAddress { get; set; }

        [JsonIgnore]
        public decimal OrderTotalPrice
        {
            get
            {
                decimal total = 0;
                foreach (var item in Items)
                {
                    total += item.Price * (decimal)item.Quantity;
                }
                return total;
            }
        }
    }
}
