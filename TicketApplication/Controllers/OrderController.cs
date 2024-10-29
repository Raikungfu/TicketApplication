using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public OrderController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Index()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var orders = await _context.Orders.Where(x => x.UserId == claimId).ToListAsync();

            return View(orders);
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult GetOrderDetails(string orderId)
        {
            var orderDetails = _context.OrderDetails
                .Where(od => od.OrderId == orderId).Include(x => x.Ticket).ThenInclude(y => y.Zone)
                .ToList();

            return PartialView("_OrderDetailsPartial", orderDetails);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IndexAdmin()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var orders = await _context.Orders.Include(x => x.OrderDetails).ThenInclude(y => y.Ticket).ToListAsync();

            return View(orders);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Checkout(string paymentMethod)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var cartItems = await _context.Carts
                .Where(c => c.UserId == claimId)
                .Include(c => c.Zone)
                .ThenInclude(z => z.Event)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return BadRequest("Giỏ hàng trống");
            }

            var order = new Order
            {
                UserId = claimId,
                Status = "Pending",
                TotalAmount = 0,
                OrderDetails = new List<OrderDetail>()
            };

            foreach (var item in cartItems)
            {
                var ticket = new Ticket
                {
                    Title = item.Zone.Event.Title,
                    Description = item.Zone.Name,
                    ZoneId = item.ZoneId,
                    Status = "Available",
                };
                await _context.Tickets.AddAsync(ticket);

                var orderDetail = new OrderDetail
                {
                    TicketId = ticket.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.Zone.Price,
                    TotalPrice = item.Zone.Price * item.Quantity
                };
                order.TotalAmount += orderDetail.TotalPrice;
                order.OrderDetails.Add(orderDetail);
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("Success");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(string orderId, decimal amount, string paymentMethod)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == claimId);

            if (order == null)
            {
                return NotFound("Đơn hàng không tồn tại.");
            }

            var payment = order.Payments ?? new Payment
            {
                OrderId = order.Id,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Status = "Completed"
            };

            if (order.Payments == null)
            {
                await _context.Payments.AddAsync(payment);
            }
            else
            {
                payment.Amount = amount;
                payment.PaymentMethod = paymentMethod;
                payment.Status = "Completed";
            }

            order.Status = "Paid";
            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            return Ok("Thanh toán thành công và đơn hàng đã được cập nhật.");
        }
    }

}

