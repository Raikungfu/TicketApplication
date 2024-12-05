using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using TicketApplication.Data;
using TicketApplication.Library;
using TicketApplication.Models;
using TicketApplication.Service;
using Net.payOS.Types;
using Net.payOS;
using Microsoft.IdentityModel.Tokens;

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


        [Authorize(Roles = "Admin")]
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
        public async Task<IActionResult> Checkout(string paymentMethod, string? orderId = null, string? code = null, string? discountOptionOrder = null)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            Order order;

            if (!orderId.IsNullOrEmpty())
            {
                order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Zone)
                    .ThenInclude(z => z.Event)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == claimId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                if (order.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Đơn hàng không hợp lệ để thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }
            }
            else
            {
                var cartItems = await _context.Carts
                    .Where(c => c.UserId == claimId)
                    .Include(c => c.Zone)
                    .ThenInclude(z => z.Event)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống.";
                    return RedirectToAction("Index", "Cart");
                }

                order = new Order
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

                    order.TotalAmount += orderDetail.TotalPrice;
                    order.OrderDetails.Add(orderDetail);
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
                }

                await _context.Orders.AddAsync(order);
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }

            decimal discountAmount = 0;
            decimal rankDiscount = 0;
            decimal codeDiscount = 0;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == claimId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return RedirectToAction("Index", "Cart");
            }

            decimal totalPrice = order.TotalAmount;

            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(discountOptionOrder) && discountOptionOrder.Equals("code"))
            {
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code == code && d.IsActive && d.ValidUntil > DateTime.Now && d.UsageLimit > 0);

                if (discount == null)
                {
                    TempData["ErrorMessage"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                    return RedirectToAction("Index", "Cart");
                }

                codeDiscount = discount.DiscountPercentage.HasValue
                    ? totalPrice * discount.DiscountPercentage.Value / 100
                    : (discount.DiscountAmount.HasValue ? discount.DiscountAmount.Value : 0);

                discount.UsageLimit--;
                _context.Discounts.Update(discount);

                order.DiscountId = discount.Id;

                _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(discountOptionOrder) && discountOptionOrder.Equals("rank") && !string.IsNullOrEmpty(user.Rank) && !user.Rank.Equals("Unknown"))
            {
                var rankPercentage = GetRankDiscountPercentage(user.Rank);
                rankDiscount = totalPrice * rankPercentage;
            }

            discountAmount = Math.Max(rankDiscount, codeDiscount);

            order.DiscountAmount = discountAmount;
            order.OrderCode = new Random().Next(100000000, 999999999);
            _context.Orders.Update(order);
            _context.SaveChangesAsync();

            var paymentLink = await createPaymentLink(Math.Max(totalPrice - discountAmount, 0), paymentMethod, order, user);
            return Redirect(paymentLink);
        }

        private async Task<string> createPaymentLink(decimal amount, string paymentMethod, Order order, User user)
        {
            string serverUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            var payOS = new PayOS(
                _configuration["PAYOS_CLIENT_ID"],
                _configuration["PAYOS_API_KEY"],
                _configuration["PAYOS_CHECKSUM_KEY"]
            );

            var paymentData = new PaymentData(
                orderCode: order.OrderCode,
                amount: (int)Math.Round(order.TotalAmount),
                description: $"TICKET ORDER {order.Id}",
                items: order.OrderDetails.Select(od => new ItemData(
                    name: $"{od.Zone.Event.Title} - {od.Zone.Name}",
                    quantity: od.Quantity,
                    price: (int)od.UnitPrice
                )).ToList(),
                returnUrl: $"{serverUrl}/confirm-payment-payos",
                cancelUrl: $"{serverUrl}/confirm-payment-payos",
                buyerName: user.Name,
                buyerEmail: user.Email,
                buyerAddress: user.Address,
                buyerPhone: user.PhoneNumber
            );

            var paymentResponse = await payOS.createPaymentLink(paymentData);
            if (paymentResponse == null || string.IsNullOrEmpty(paymentResponse.checkoutUrl))
            {
                throw new Exception("Failed to create payment link");
            }

            return paymentResponse.checkoutUrl;
        }

        [HttpGet("confirm-payment-payos")]
        public async Task<IActionResult> ConfirmPaymentPayOS([FromQuery] string code, [FromQuery] string id, [FromQuery] bool cancel, [FromQuery] string status, [FromQuery] long orderCode)
        {
            string serverUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            
            try
            {
                if (code.Equals("01"))
                {
                    TempData["ErrorMessage"] = "Invalid Params!";
                    return RedirectToAction("Index", "Order");
                }

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                    .ThenInclude(od => od.Zone)
                    .ThenInclude(od => od.Event)
                    .Include(o => o.Payments)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found!";
                    return RedirectToAction("Index", "Order");
                }


                var amount = Math.Max(order.TotalAmount - order.DiscountAmount ?? 0, 0);
                if (cancel || status == "CANCELLED")
                {
                    var payment = CreateOrUpdatePayment(order, amount, "Failed");

                    TempData["ErrorMessage"] = "Payment failed!";
                    return RedirectToAction("Index", "Order");
                }
                else if (status == "PAID")
                {
                    order.Status = "Paid";
                    var payment = CreateOrUpdatePayment(order, amount, "Paid");

                    foreach (var orderDetail in order.OrderDetails)
                    {
                        var zone = await _context.Zones.FindAsync(orderDetail.ZoneId);
                        if (zone != null)
                        {
                            zone.AvailableTickets -= orderDetail.Quantity;
                            _context.Zones.Update(zone);

                            foreach (var ticket in orderDetail.Tickets)
                            {
                                ticket.Status = "Sold";
                                _context.Tickets.Update(ticket);
                            }
                        }
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment failed!";
                    return RedirectToAction("Index", "Order");
                }

                order.Payments.PaymentRef = id;
                order.Payments.CreatedAt = DateTime.Now;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                _emailService.SendTicketOrderConfirmationMail(order.User.Email, order.User.Name, order);
                TempData["SuccessMessage"] = "Thanh toán thành công, Ticket đã được gửi đến email của bạn.";
                return RedirectToAction("Index", "Order");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Order");
            }
        }

        private Payment CreateOrUpdatePayment(Order order, decimal amount, string status)
        {
            var payment = order.Payments ?? new Payment
            {
                OrderId = order.Id,
                Amount = amount,
                PaymentMethod = "PayOS",
                Status = status
            };

            payment.Amount = amount;
            payment.PaymentMethod = "PayOS";
            payment.Status = status;

            if (order.Payments == null)
            {
                _context.Payments.Add(payment);
            }
            else
            {
                _context.Payments.Update(payment);
            }

            return payment;
        }


        [HttpPost("confirm-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmWebhook([FromBody] Dictionary<string, string> requestBody)
        {
            var response = new
            {
                error = 0,
                message = "ok",
                data = ""
            };

            var payOS = new PayOS(
                _configuration["PAYOS_CLIENT_ID"],
                _configuration["PAYOS_API_KEY"],
                _configuration["PAYOS_CHECKSUM_KEY"]
            );

            try
            {
                if (requestBody.TryGetValue("webhookUrl", out var webhookUrl))
                {

                    var str = await payOS.confirmWebhook(webhookUrl);
                    response = new
                    {
                        error = 0,
                        message = "ok",
                        data = str
                    };
                }
                else
                {
                    response = new
                    {
                        error = -1,
                        message = "Missing 'webhookUrl' parameter.",
                        data = ""
                    };
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                response = new
                {
                    error = -1,
                    message = ex.Message,
                    data = ""
                };
                return StatusCode(500, response);
            }
        }


        public static string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);

            Regex regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
            string withoutDiacritics = regex.Replace(normalizedString, "");

            return withoutDiacritics.Normalize(NormalizationForm.FormC);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyDiscount([FromBody] ApplyDiscountRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) && string.IsNullOrEmpty(request.Rank))
            {
                return Json(new { success = false, message = "Bạn phải chọn một mã giảm giá hoặc giảm giá theo Rank." });
            }

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == claimId);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == claimId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại hoặc không thuộc về bạn." });
            }

            var totalPrice = order.TotalAmount;
            decimal discountAmount = 0;
            decimal rankDiscount = 0;
            decimal codeDiscount = 0;

            if (string.IsNullOrEmpty(request.Code) && !string.IsNullOrEmpty(user.Rank))
            {
                var rankPercentage = GetRankDiscountPercentage(user.Rank);
                rankDiscount = totalPrice * rankPercentage;
            }

            else if (!string.IsNullOrEmpty(request.Code))
            {
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code == request.Code && d.IsActive && d.ValidUntil > DateTime.Now && d.UsageLimit > 0);

                if (discount == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
                }

                codeDiscount = discount.DiscountPercentage.HasValue
                    ? totalPrice * discount.DiscountPercentage.Value / 100
                    : (discount.DiscountAmount.HasValue ? discount.DiscountAmount.Value : 0);
            }

            discountAmount = Math.Max(rankDiscount, codeDiscount);
            var newTotal = Math.Max(totalPrice - discountAmount, 0);

            return Json(new
            {
                success = true,
                newTotal = newTotal.ToString("N0"),
                discountAmount = discountAmount.ToString("N0"),
                message = $"Bạn đã được giảm {discountAmount.ToString("N0")} đ. Tổng tiền mới của đơn hàng là {newTotal.ToString("N0")} đ.",
                appliedRank = rankDiscount > codeDiscount ? user.Rank : null,
                appliedCode = codeDiscount > rankDiscount ? request.Code : null
            });
        }



        private decimal GetRankDiscountPercentage(string rank)
        {
            return rank switch
            {
                "Đồng" => 0.02m,
                "Bạc" => 0.04m,
                "Vàng" => 0.06m,
                "Bạch Kim" => 0.08m,
                _ => 0m
            };
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Delete(string orderId)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == claimId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc không thuộc về bạn.";
                return RedirectToAction(nameof(Index));
            }

            if (order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Đơn hàng không thể xoá vì trạng thái không phải là 'Pending'.";
                return RedirectToAction(nameof(Index));
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn hàng đã được xoá thành công.";
            return RedirectToAction(nameof(Index));
        }


    }


    public class ApplyDiscountRequest
    {
        public string OrderId { get; set; }
        public string Code { get; set; }
        public string Rank { get; set; }
    }

    public class PayOSResponse
    {
        public bool loading { get; set; }
        public string code { get; set; }
        public string id { get; set; }
        public string cancel { get; set; }
        public int orderCode { get; set; }
        public string status { get; set; }
    }

    public class OrderPaymentViewModel
    {
        public string? PaymentMethod { get; set; }
        public string OrderId { get; set; }
        public string? DiscountMethod { get; set; }
        public string? DiscountCode { get; set; }
    }
}