using Development.Blazor.Client.DTOs.Accounts;
using Development.Blazor.Client.DTOs.Carts;

namespace Development.Blazor.Client.DTOs.Orders
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
