using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    [Authorize]
    public class CartController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public CartController(ILogger<HomeController> logger, ApplicationDbContext context)
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

            var cart = await _context.Carts.Where(x => x.UserId == claimId).Include(x => x.Zone).ThenInclude(y => y.Event).ToListAsync();

            return View(cart);
        }

        public IActionResult QuantityItems()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId != null)
            {
                var quantity = _context.Carts.Where(x => x.UserId == claimId).Sum(x => x.Quantity);
                return Ok(new { quantity });
            }
            return BadRequest();
        }

        [HttpDelete]
        public IActionResult RemoveItem(string itemId)
        {
            var cartItem = _context.Carts.Include(x => x.Zone).FirstOrDefault(c => c.Zone.Id == itemId);

            decimal priceRemove = cartItem.TotalPrice;

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();

                return Ok(new { success = true, message = "Remove success", priceRemove });
            }

            return NotFound();
        }

        [HttpPut]
        public IActionResult UpdateQuantity(string itemId, [FromBody] UpdateQuantityModel model)
        {

            var cartItem = _context.Carts.Include(c => c.Zone).FirstOrDefault(c => c.Zone.Id == itemId);


            if (model.Quantity > cartItem.Zone.AvailableTickets)
            {
                return BadRequest(new { success = false, message = "Không đủ số lượng! Chỉ còn " +cartItem.Zone.AvailableTickets + " vé"  });
            }

            if (cartItem != null)
            {
                cartItem.Quantity = model.Quantity;
                _context.SaveChanges();
                var updatedTotalPrice = cartItem.Quantity * cartItem.Zone.Price;
                var newGrandTotal = _context.Carts.Sum(c => c.Quantity * c.Zone.Price);
                return Ok(new { success = true, message = "Update quantity success",
                    updatedTotalPrice = updatedTotalPrice, 
                    newGrandTotal = newGrandTotal
                });
            }

            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(string itemId, [FromBody] AddToCartModel model)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var existingCartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == claimId && c.Zone != null && c.Zone.Id == itemId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += model.Quantity;
            }
            else
            {
                var newCartItem = new Cart
                {
                    UserId = claimId,
                    ZoneId = itemId,
                    Quantity = model.Quantity
                };
                await _context.Carts.AddAsync(newCartItem);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        /*
        [HttpPost]
        public async Task<IActionResult> ApplyDiscount([FromBody] DiscountRequest request)
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return Json(new { success = false, message = "Mã giảm giá không được để trống." });
            }

            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code == request.Code && d.IsActive && d.ValidUntil > DateTime.Now && d.UsageLimit > 0);

            if (discount == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts.Where(x => x.UserId == claimId).Include(x => x.Zone.Price).ToListAsync();
            if (cart == null || !cart.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống." });
            }

            var totalPrice = cart.Sum(i => i.TotalPrice);


            var newTotal = totalPrice - discountAmount;
            newTotal = Math.Max((decimal) newTotal, 0);

            return Json(new
            {
                success = true,
                newTotal,
                discountAmount
            });
        }*/

        [HttpPost]
        public async Task<IActionResult> ApplyDiscount([FromBody] DiscountRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) && string.IsNullOrEmpty(request.Rank))
            {
                return Json(new { success = false, message = "Bạn phải chọn một mã giảm giá" });
            }

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == claimId);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            var cart = await _context.Carts.Where(x => x.UserId == claimId).Include(c => c.Zone).ToListAsync();
            if (cart == null || !cart.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống." });
            }

            var totalPrice = cart.Sum(i => i.TotalPrice);

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

                codeDiscount = discount.DiscountPercentage.HasValue ? totalPrice * discount.DiscountPercentage.Value / 100 : (discount.DiscountAmount.HasValue ? discount.DiscountAmount.Value : 0);

                /* discount.UsageLimit--;
                _context.Discounts.Update(discount);
                await _context.SaveChangesAsync(); */
            }

            // Người dùng chỉ được chọn một trong hai
            discountAmount = Math.Max(rankDiscount, codeDiscount);
            var newTotal = Math.Max(totalPrice - discountAmount, 0);

            return Json(new
            {
                success = true,
                newTotal = newTotal.ToString("N0"),
                discountAmount = discountAmount.ToString("N0"),
                message = $"Bạn đã được giảm {discountAmount.ToString("N0")} đ. Tổng tiền mới là {newTotal.ToString("N0")} đ.",
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


    }

    public class UpdateQuantityModel
    {
        public int Quantity { get; set; }
    }

    public class AddToCartModel
    {
        public int Quantity { get; set; }
    }

    public class DiscountRequest
    {
        public string Code { get; set; }
        public string Rank { get; set; }
    }

}