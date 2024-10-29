using ClothesStoreMobileApplication.Library;
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
        private readonly IConfiguration _configuration;

        public OrderController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
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

            var orders = await _context.Orders.Include(x => x.User).Include(x => x.OrderDetails).ThenInclude(y => y.Ticket).ThenInclude(s => s.Zone).ToListAsync();

            return View(orders);
        }


        public async Task<IActionResult> Edit(string orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = newStatus;
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(IndexAdmin)); 
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

            var paymentLink = createPaymentLink(order.TotalAmount, paymentMethod, order.Id);
            return Redirect(paymentLink);
        }

        public string createPaymentLink(decimal TotalPrice, string paymentMethod, string orderId)
        {
            string vnp_ReturnUrl = _configuration["VnPay:PaymentBackReturnUrl"];
            string vnp_Url = _configuration["VnPay:BaseURL"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (TotalPrice * 100).ToString());

            if (paymentMethod == "DomesticCard")
            {
                vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            }
            else if (paymentMethod == "InternationalCard")
            {
                vnpay.AddRequestData("vnp_BankCode", "INTCARD");
            }

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + orderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", orderId);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmPayment()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var queryParams = Request.Query;

            Dictionary<string, string> queryDictionary = queryParams.ToDictionary(q => q.Key, q => q.Value.ToString());

            /*
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
            
            
            */
            await _context.SaveChangesAsync();

            return Ok("Thanh toán thành công và đơn hàng đã được cập nhật.");
        }
    }

}

