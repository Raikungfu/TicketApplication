using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Library;
using TicketApplication.Models;
using TicketApplication.Service;

namespace TicketApplication.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public OrderController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration, EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Index()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var orders = await _context.Orders.Where(x => x.UserId == claimId).Include(x => x.User).Include(x => x.OrderDetails).ThenInclude(y => y.Tickets).ThenInclude(s => s.Zone).ToListAsync();

            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IndexAdmin()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var orders = await _context.Orders.Include(x => x.User).Include(x => x.OrderDetails).ThenInclude(y => y.Tickets).ThenInclude(s => s.Zone).ToListAsync();

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
                TempData["ErrorMessage"] = "Giỏ Hàng Trống";
                return RedirectToAction("Index", "Cart");
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


                var orderDetail = new OrderDetail
                {
                    Quantity = item.Quantity,
                    UnitPrice = item.Zone.Price,
                    TotalPrice = item.Zone.Price * item.Quantity,
                    ZoneId = item.ZoneId,
                    OrderId = order.Id
                };

                await _context.OrderDetails.AddAsync(orderDetail);

                for (int i = 0; i < item.Quantity; i++)
                {
                    var ticket = new Ticket
                    {
                        Title = item.Zone.Event.Title,
                        Description = item.Zone.Name,
                        ZoneId = item.ZoneId,
                        Status = "Available",
                        OrderDetailId = orderDetail.Id
                    };
                    await _context.Tickets.AddAsync(ticket);
                }

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

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CheckoutOrder(string orderId, string paymentMethod)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

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
            vnpay.AddRequestData("vnp_Amount", ((double) TotalPrice * 100).ToString());

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
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + orderId);
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

            var requiredParams = new[] { "vnp_TxnRef", "vnp_ResponseCode", "vnp_SecureHash", "vnp_Amount", "vnp_BankCode" };

            foreach (var param in requiredParams)
            {
                if (!queryParams.ContainsKey(param))
                {
                    return BadRequest($"Thiếu tham số: {param}");
                }
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!referer.StartsWith("https://sandbox.vnpayment.vn") && !referer.StartsWith("https://www.vnpayment.vn"))
            {
                return BadRequest("Yêu cầu không hợp lệ.");
            }

            Dictionary<string, string> queryDictionary = queryParams.ToDictionary(q => q.Key, q => q.Value.ToString());

            string vnp_TxnRef = queryDictionary["vnp_TxnRef"];
            string vnp_ResponseCode = queryDictionary["vnp_ResponseCode"];
            string vnp_SecureHash = queryDictionary["vnp_SecureHash"];
            decimal amount = (decimal)Convert.ToDouble(queryDictionary["vnp_Amount"]);
            string paymentMethod = queryDictionary["vnp_BankCode"];

            if (!VerifySecureHash(vnp_SecureHash))
            {
                return BadRequest("Secure hash không hợp lệ.");
            }

            var order = await _context.Orders.Include(o => o.Payments).Include(x => x.OrderDetails).ThenInclude(y => y.Tickets).ThenInclude(y => y.Zone.Event)
                .FirstOrDefaultAsync(o => o.Id == vnp_TxnRef && o.UserId == claimId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại.";
                return NotFound("Đơn hàng không tồn tại.");
            }

            if (vnp_ResponseCode != "00")
            {
                TempData["ErrorMessage"] = "Thanh toán không thành công";
                return RedirectToAction("Index", "Order");
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

            foreach (var orderDetail in order.OrderDetails)
            {
                var zone = await _context.Zones.FindAsync(orderDetail.ZoneId);
                if (zone != null)
                {
                    zone.AvailableTickets -= orderDetail.Quantity;
                    _context.Zones.Update(zone);
                }
            }

            order.Status = "Paid";
            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(claimId);

            _emailService.SendTicketOrderConfirmationMail(user.Email, user.Name, order);
            TempData["SuccessMessage"] = "Thanh toán thành công, Ticket đã được gửi đến email của bạn.";
            return RedirectToAction("Index", "Order");
        }

        private bool VerifySecureHash(string secureHash)
        {
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.ValidateSignature(secureHash, vnp_HashSecret);
            return true;
        }
    }
}