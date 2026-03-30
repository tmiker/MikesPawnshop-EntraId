using Orders.API.DTOs;
using Orders.API.Paging;

namespace Orders.API.Abstractions
{
    public interface IOrderService
    {
        Task<ReviewOrderResultDTO> ReviewOrderAsync(AddOrderDTO addOrderDTO, string ownerId, CancellationToken cancellationToken);
        Task<(bool IsSuccess, IEnumerable<OrderDTO>? OrderDTOs, PaginationMetadata? PagingData, string? ErrorMessage)> GetAllUserOrdersAsync(string ownerId, string? filter, int pageNumber, int pageSize, string? sortColumn = null, string? sortOrder = null);
        Task<(bool IsSuccess, OrderDTO? OrderDTO, string? ErrorMessage)> GetOrderByOrderIdAsync(string ownerId, string orderId);
        Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> AddOrderAsync(string ownerId, AddOrderDTO addOrderDTO, CancellationToken cancellationToken);
        Task<(bool IsSuccess, string? ErrorMessage)> UpdateOrderAsync(string ownerId, UpdateOrderDTO updateOrderDTO);
        Task<(bool IsSuccess, string? ErrorMessage)> CancelOrderAsync(string ownerId, string orderId);
    }
}
