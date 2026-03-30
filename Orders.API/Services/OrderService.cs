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
        private readonly IMongoCollection<Order> _orders;
        private readonly IInternalAccountsHttpService _internalAccountsHttpService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IMongoSettings mongoSettings, InternalAccountsHttpService internalAccountsHttpService, ILogger<OrderService> logger)
        {
            var client = new MongoClient(mongoSettings.MongoLocalConnection);
            var database = client.GetDatabase(mongoSettings.Database);
            _orders = database.GetCollection<Order>(mongoSettings.OrderCollection);
            _internalAccountsHttpService = internalAccountsHttpService;
            _logger = logger;
        }

        public async Task<ReviewOrderResultDTO> ReviewOrderAsync(AddOrderDTO addOrderDTO, string ownerId, CancellationToken cancellationToken)
        {
            ReviewOrderResultDTO resultDTO = new ReviewOrderResultDTO();

            /// 1. Get account dto using internal accounts http services - note the http service will add api key for internal account api call and encrypt data
            var accountResult = await _internalAccountsHttpService.GetUserAccountDataAsync();
            if (accountResult.IsSuccess)
            {
                resultDTO.AccountId = accountResult.AccountDTO?.AccountId;
                resultDTO.AccountStatus = accountResult.AccountDTO?.AccountStatus;
            }
            else
            {
                string errorMessage = accountResult.ErrorMessage ??  "Unknown error retrieving account details.";
                resultDTO.ErrorMessages.Add(errorMessage);
            }

            /// 2. Review the account and order for completeness and consistnecy
            if (resultDTO.AccountStatus?.ToLower() == "hold") resultDTO.ErrorMessages.Add("The account is on hold.");
            if (addOrderDTO.Items is null || addOrderDTO.Items.Count == 0) resultDTO.ErrorMessages.Add("The order contains no items.");
            if (addOrderDTO.ShippingAddress is null) resultDTO.ErrorMessages.Add("The order is missing a shipping address.");
            if (addOrderDTO.BillingAddress is null) resultDTO.ErrorMessages.Add("The order is missing a billing address.");

            /// 3. Return cumumlative result
            return resultDTO;
        }

        public async Task<(bool IsSuccess, string? OrderId, string? ErrorMessage)> AddOrderAsync(string ownerId, AddOrderDTO addOrderDTO, CancellationToken cancellationToken)
        {
            // SET ORDER ID HERE
            // string orderId = Guid.NewGuid().ToString();              // done by domain model

            Address? shipping = addOrderDTO.ShippingAddress is null ? null : new Address(addOrderDTO.ShippingAddress);
            Address? billing = addOrderDTO.BillingAddress is null ? null : new Address(addOrderDTO.BillingAddress);
            // Ensure order addresses are in user account addresses
            // ...

            List<OrderItem> items = new List<OrderItem>();
            addOrderDTO.Items.ForEach(itemDTO => items.Add(new OrderItem(itemDTO)));
            // int lineNumber = 1;
            // items.ForEach(i => i.LineNumber = lineNumber++);         // done by domain model
            // items.ForEach(i => i.OrderId = orderId);                 // done by domain model
            Order order = new Order(ownerId, items, shipping, billing);
            await _orders.InsertOneAsync(order);

            return (true, order.OrderId, null);

            //// NEED TO REMOVE CART IF SUCCESS 
            //var deleteCartResult = await _internalOrdersService.RemoveShoppingCartAsync(ownerId, cancellationToken);
            //if (deleteCartResult.IsSuccess)
            //{
            //    _logger.LogInformation("User cart removed after order submission.");
            //    return (true, order.OrderId, null);
            //}
            //else return (false, order.OrderId, deleteCartResult.ErrorMessage);
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

        //public async Task<(bool IsSuccess, ReviewOrderResultDTO? ReviewDTO, string? ErrorMessage)> ReviewOrderAsync(string ownerId, CancellationToken cancellationToken)
        //{
            
        //    //// THIS ENDPOINT MOVED TO INTERNAL ORDERS SERVICE

        //    //string errors = string.Empty;
        //    //ReviewOrderResultDTO resultDTO = new ReviewOrderResultDTO();

        //    //// ORIGINAL
        //    //// 1. Get account detail dto from internal accounts services - note the http service will add api key for internal account api call and encrypt data
        //    //var accountResult = await _internalAccountsHttpService.GetAccountDetailsFromInternalApiAsync(ownerId);
        //    //if (accountResult.IsSuccess)
        //    //{
        //    //    resultDTO.AccountOwnerId = accountResult.AccountDetail?.AccountOwnerId;
        //    //    resultDTO.AccountDetail = accountResult.AccountDetail;
        //    //}
        //    //else errors += $"{accountResult.ErrorMessage} \n";

        //    //// 2. Get cart from internal cart services - note the http service will add api key for internal cart api call and encrypt data

        //    //var cartResult = await _internalCartsHttpService.GetShoppingCartAsync(ownerId);
        //    //if (cartResult.IsSuccess)
        //    //{
        //    //    resultDTO.ShoppingCart = cartResult.ShoppingCart;
        //    //}
        //    //else errors += $"{cartResult.ErrorMessage}";

        //    //if (string.IsNullOrWhiteSpace(errors)) return (true, resultDTO, null);
        //    //else return (false, resultDTO, errors);
        //}
    }
}
