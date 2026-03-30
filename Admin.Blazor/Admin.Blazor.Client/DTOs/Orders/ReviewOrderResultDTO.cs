using Admin.Blazor.Client.DTOs.Accounts;
using Admin.Blazor.Client.DTOs.Carts;

namespace Admin.Blazor.Client.DTOs.Orders
{
    public class ReviewOrderResultDTO
    {
        public bool IsSuccess { get; set; }

        public string? AccountId { get; set; }
        public string? AccountStatus { get; set; }

        public List<string> ErrorMessages { get; set; } = new List<string>();

        //// public OrderDTO? OrderToReview { get; set; }
        //public AccountDTO? Account { get; set; }
        //public ShoppingCartDTO? ShoppingCart { get; set; }

        //// dev test only
        //public string? AccountOwnerId { get; set; }
        //public string? CartOwnerId { get; set; }

        //public bool IsSuccess { get; set; }
        //public string? ErrorMessage { get; set; }
    }
}
