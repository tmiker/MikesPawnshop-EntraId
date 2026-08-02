using Admin.Blazor.Client.DTOs.Accounts;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Admin.Blazor.Client.DTOs.Orders
{
    public class AddOrderDTO
    {
        [Required]
        public List<AddOrderItemDTO> Items { get; set; } = new List<AddOrderItemDTO>();
        [Required]
        public AddressDTO? ShippingAddress { get; set; }
        [Required]
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
