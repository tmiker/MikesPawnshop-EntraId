using Consumer.Blazor.Client.DTOs.Orders;
using Consumer.Blazor.Client.Paging;

namespace Consumer.Blazor.Client.Abstractions
{
    public interface IOrdersHttpService
    {
        Task<(bool IsSuccess, ReviewOrderResultDTO? ReviewOrderResult, string? ErrorMessage)> ReviewOrderAsync();
        Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> SubmitOrderAsync(AddOrderDTO addOrderDTO);
        Task<(bool IsSuccess, IEnumerable<OrderDTO>? OrderDTOs, PaginationMetadata? PagingData, string? ErrorMessage)> GetAllUserOrdersAsync(
            string? filter = null, string? sortColumn = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10);
        Task<(bool IsSuccess, OrderDTO? OrderDTO, string? ErrorMessage)> GetOrderByOrderIdAsync(string orderId);
    }
}
