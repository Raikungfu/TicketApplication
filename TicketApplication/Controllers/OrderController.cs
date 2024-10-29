using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    [AllowAnonymous]
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

            var orders = await _context.Orders.Where(x => x.UserId == claimId).Include(x => x.OrderDetails).ThenInclude(y => y.Zone).ToListAsync();

            return View(orders);
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Checkout(List<int> quantities)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var cartItems = _context.Carts.ToList();
            var order = new Order
            {
                UserId = claimId,
                Status = "Pending",
                TotalAmount = 0
            };
            
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ZoneId = item.Zone.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.Zone.Price,
                    TotalPrice = item.Zone.Price * item.Quantity
                };

                order.TotalAmount += orderDetail.TotalPrice;
                order.OrderDetails.Add(orderDetail);
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            _context.Carts.RemoveRange(cartItems);
            _context.SaveChanges();

            return RedirectToAction("Success");
        }

    }
}
