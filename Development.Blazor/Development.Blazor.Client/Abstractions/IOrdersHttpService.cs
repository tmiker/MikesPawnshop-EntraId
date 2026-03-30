using Development.Blazor.Client.DTOs;
using Development.Blazor.Client.DTOs.Orders;
using Development.Blazor.Client.Paging;

namespace Development.Blazor.Client.Abstractions
{
    public interface IOrdersHttpService
    {
        Task<(bool IsSuccess, ApiUserInfoDTO? ApiUserInfo, string? ErrorMessage)> GetOrdersApiUserInfoAsync(string? token = null);
        Task<(bool IsSuccess, ReviewOrderResultDTO? ReviewOrderResult, string? ErrorMessage)> ReviewOrderAsync(AddOrderDTO addOrderDTO, string? token = null);
        Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> SubmitOrderAsync(AddOrderDTO addOrderDTO, string? token = null);
        Task<(bool IsSuccess, IEnumerable<OrderDTO>? OrderDTOs, PaginationMetadata? PagingData, string? ErrorMessage)> GetAllUserOrdersAsync(
            string? filter = null, string? sortColumn = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10);
        Task<(bool IsSuccess, OrderDTO? OrderDTO, string? ErrorMessage)> GetOrderByOrderIdAsync(string orderId);
    }
}
