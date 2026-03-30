namespace Orders.API.DTOs
{
    public class AddOrderDTO
    {
        public List<AddOrderItemDTO> Items { get; set; } = new List<AddOrderItemDTO>();
        public AddressDTO? ShippingAddress { get; set; }
        public AddressDTO? BillingAddress { get; set; }
    }
}
