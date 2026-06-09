using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.API.Abstractions;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;
        private readonly ITokenDecoder _tokenDecoder;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger, ITokenDecoder tokenDecoder)
        {
            _orderService = orderService;
            _logger = logger;
            _tokenDecoder = tokenDecoder;
        }

        [HttpGet("count")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> GetOrderCount()
        {
            var result = await _orderService.GetOrderCountAsync();
            if (result.IsSuccess) return Ok(result.OrderCount);
            return BadRequest();
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostOrder(AddOrderDTO addOrderDTO)
        {
            // await LogIdentityInformation();
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            var result = await _orderService.AddOrderAsync(ownerId, addOrderDTO, tokenSource.Token);
            if (result.IsSuccess)
            {
                return Ok(result.OrderId);
            }
            else
            {
                return BadRequest(result.ErrorMessage);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetUserOrders(string? filter = null, string? sortColumn = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            var orderResult = await _orderService.GetAllUserOrdersAsync(ownerId, filter, pageNumber, pageSize, sortColumn, sortOrder);
            if (orderResult.IsSuccess)
            {
                PagedOrderResultDTO resultDTO = new PagedOrderResultDTO() { OrderDTOs = orderResult.OrderDTOs, PagingData = orderResult.PagingData };
                return Ok(resultDTO);
            }
            else
            {
                return BadRequest(orderResult.ErrorMessage);
            }
        }

        [HttpGet("{orderId}")]
        [Authorize]
        public async Task<ActionResult<OrderDTO?>> GetOrderByOrderId(string orderId)
        {
            string? ownerId = User.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            if (ownerId == null) throw new InvalidUserCredentitalsException($"User identity information unavailable. Unauthorized access to restricted resource.");

            var orderResult = await _orderService.GetOrderByOrderIdAsync(ownerId, orderId);
            if (orderResult.IsSuccess) return Ok(orderResult.OrderDTO);
            else return BadRequest(orderResult.ErrorMessage);
        }
    }
}
