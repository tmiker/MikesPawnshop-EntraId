using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Orders.API.Abstractions;
using Orders.API.Domain.Models;
using Orders.API.DTOs;
using Orders.API.Paging;
using Orders.API.Utility;

namespace Orders.API.Services
{
    public class OrderService : IOrderService
    {
        private readonly IConfiguration _config;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IMongoCollection<Order> _orders;
        private readonly IInternalAccountsHttpService _internalAccountsHttpService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IConfiguration config,
            IEncryptionHelper encryptionHelper,
            IInternalAccountsHttpService internalAccountsHttpService,
            ILogger<OrderService> logger)
        {
            _config = config;
            _encryptionHelper = encryptionHelper;
            string? environment = _config["ASPNETCORE_ENVIRONMENT"];
            var client = environment == "Development" ? new MongoClient(_config["LOCAL_MONGO_CONNECTION"]) :
                new MongoClient(_config["AZURE_MONGO_CONNECTION"]);
            var database = client.GetDatabase(_config["MONGO_DATABASE"]);
            _orders = database.GetCollection<Order>(_config["MONGO_ORDER_COLLECTION"]);
            _internalAccountsHttpService = internalAccountsHttpService;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> AddOrderAsync(string ownerId, AddOrderDTO addOrderDTO, CancellationToken cancellationToken)
        {
            // 1. Check Account Status and get Shipping and Billing Address, return with errors if any
            AccountStatusResponseDTO accountStatusDTO = await GetAccountStatusAsync(ownerId, cancellationToken);
            int errorCount = accountStatusDTO.Errors is null ? 0 : accountStatusDTO.Errors.Count;
            _logger.LogInformation("{this}: Account status retrieval complete for order review. Account Status: {status}, Account Error Count: {count} ***", this.GetType().Name, accountStatusDTO.Status, errorCount);
            if (errorCount > 0) return (false, null, $"Error retrieving account status. Errors: {string.Join(" | ", accountStatusDTO.Errors!)}");

            // 2. Check AddOrderDTO for presence of order items and return with errors if none
            if (addOrderDTO.Items is null || addOrderDTO.Items.Count == 0) return (false, null, "No order items were found in the place order request.");

            // 3. If no errors, place new order
            // ORDER ID AND SETTING OF LINE NUMBERS IS HANDLED BY DOMAIN MODEL
            List<OrderItem> items = new List<OrderItem>();
            addOrderDTO.Items.ForEach(itemDTO => items.Add(new OrderItem(itemDTO)));
            Address shippingAddress = new Address(accountStatusDTO.ShippingAddress!);   // if accountStatusDTO.ShippingAddress is null, error result is returned above
            Address billingAddress = new Address(accountStatusDTO.BillingAddress!);     // if accountStatusDTO.BillingAddress is null, error result is returned above
            Order order = new Order(ownerId, items, shippingAddress, billingAddress);
            await _orders.InsertOneAsync(order);

            return (true, order.OrderId, null);

            //// FOR CART REMOVAL ON SUCCESS PLACING ORDER - THIS IS CURRENTLY THE RESPONSIBILITY OF THE FRONT END CLIENT
            //var deleteCartResult = await _internalOrdersService.RemoveShoppingCartAsync(ownerId, cancellationToken);
            //if (deleteCartResult.IsSuccess)
            //{
            //    _logger.LogInformation("User cart removed after order submission.");
            //    return (true, order.OrderId, null);
            //}
            //else return (false, order.OrderId, deleteCartResult.ErrorMessage);
        }

        private async Task<AccountStatusResponseDTO> GetAccountStatusAsync(string ownerId, CancellationToken cancellationToken)
        {
            AccountStatusResponseDTO responseDTO = new AccountStatusResponseDTO();
            AccountStatusRequestDTO statusRequestDTO = new AccountStatusRequestDTO();

            try
            {
                string aesKey = _config["IntAcctsAesSymEncryption_Key"] ?? throw new InvalidOperationException("AES key is not configured.");
                string aesIV = _config["IntAcctsAesSymEncryption_IV"] ?? throw new InvalidOperationException("AES IV is not configured.");
                var encryptedOwnerId = _encryptionHelper.Encrypt(ownerId, aesKey, aesIV);
                statusRequestDTO = new AccountStatusRequestDTO() { EncryptedOwnerId = encryptedOwnerId };  
            }
            catch (Exception ex)
            {
                responseDTO.Errors.Add($"Error encrypting required data. {ex.Message}");
                return responseDTO;
            }

            responseDTO = await _internalAccountsHttpService.GetUserAccountStatusAsync(statusRequestDTO, cancellationToken);
            return responseDTO;
        }

        public async Task<(bool IsSuccess, IEnumerable<OrderDTO>? OrderDTOs, PaginationMetadata? PagingData, string? ErrorMessage)> GetAllUserOrdersAsync(string ownerId, string? filter, int pageNumber, int pageSize, string? sortColumn = null, string? sortOrder = null)
        {
            if (pageNumber == 0) pageNumber = 1;

            IQueryable<Order> query = _orders.AsQueryable();
            query = query.Where(o => o.OwnerId == ownerId);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filter = filter.Trim().ToLowerInvariant();
                query = query.Where(o => o.OrderId!.ToLowerInvariant() == filter);
            }
            if (!string.IsNullOrWhiteSpace(sortColumn))
            {
                sortOrder = sortColumn.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(sortOrder))
                {
                    if (sortOrder.ToLowerInvariant().Contains("desc"))
                    {
                        switch (sortColumn)
                        {
                            case "orderid":
                                query = query.OrderByDescending(o => o.OrderId);
                                break;
                            case "orderdate":
                                query = query.OrderByDescending((o) => o.OrderDate);
                                break;
                        }
                    }
                    else
                    {
                        switch (sortColumn)
                        {
                            case "orderid":
                                query = query.OrderBy(o => o.OrderId);
                                break;
                            case "orderdate":
                                query = query.OrderBy((o) => o.OrderDate);
                                break;
                        }
                    }
                }
            }
            else query = query.OrderByDescending((o) => o.OrderDate);

            long ordersCount = await _orders.Find(o => o.OwnerId == ownerId).CountDocumentsAsync();
            List<Order> orders = await query.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToListAsync();

            PaginationMetadata metadata = new PaginationMetadata((int)ordersCount, pageSize, pageNumber);

            List<OrderDTO> orderDTOs = new List<OrderDTO>();
            orders.ForEach(o => orderDTOs.Add(o.ToDTO()));

            return (true, orderDTOs, metadata, null);
        }

        public async Task<(bool IsSuccess, OrderDTO? OrderDTO, string? ErrorMessage)> GetOrderByOrderIdAsync(string ownerId, string orderId)
        {
            Order order = await _orders.Find(o => o.OwnerId == ownerId && o.OrderId == orderId).FirstOrDefaultAsync();
            if (order != null)
            {
                OrderDTO orderDTO = order.ToDTO();
                return (true, orderDTO, null);
            }
            return (false, null, $"An order with OrderId {orderId} not found.");
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> UpdateOrderAsync(string ownerId, UpdateOrderDTO updateOrderDTO)
        {
            var order = await _orders.Find(o => o.OwnerId == ownerId && o.OrderId == updateOrderDTO.OrderId).FirstOrDefaultAsync();
            if (order != null)
            {
                List<OrderItem> items = new List<OrderItem>();
                updateOrderDTO.Items.ForEach(i => items.Add(new OrderItem(i)));
                order.Status = StaticData.OrderStatus_Updated;
                order.Version++;
                var result = await _orders.ReplaceOneAsync(p => p.OrderId == updateOrderDTO.OrderId, order);
                return (true, null);
            }
            return (false, $"Error updating order.");
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> CancelOrderAsync(string ownerId, string orderId)
        {
            var order = await _orders.Find(o => o.OwnerId == ownerId && o.OrderId == orderId).FirstOrDefaultAsync();
            if (order is null)
            {
                return (false, $"An order with OrderId {orderId} was not found.");
            }
            DeleteResult result = await _orders.DeleteOneAsync(o => o.OrderId == orderId);
            if (result.DeletedCount > 0) return (true, null);
            return (false, $"An order was found, but was not deleted.");
        }
    }
}
