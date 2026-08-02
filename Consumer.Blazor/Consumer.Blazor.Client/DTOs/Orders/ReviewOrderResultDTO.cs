using Consumer.Blazor.Client.DTOs.Accounts;
using Consumer.Blazor.Client.DTOs.Carts;

namespace Consumer.Blazor.Client.DTOs.Orders
{
    public class ReviewOrderResultDTO
    {
        public AccountDTO? Account { get; set; }
        public ShoppingCartDTO? ShoppingCart { get; set; }

        // dev test only
        public string? AccountOwnerId { get; set; }
        public string? CartOwnerId { get; set; }

        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
