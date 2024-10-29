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

        [HttpDelete]
        public IActionResult RemoveItem(string id)
        {
            var cartItem = _context.Carts.FirstOrDefault(c => c.Zone.Id == id);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();

                return Ok(new { success = true, message = "Remove success" });
            }

            return NotFound();
        }

        [HttpPut]
        public IActionResult UpdateQuantity(string itemId, [FromBody] UpdateQuantityModel model)
        {
            var cartItem = _context.Carts.Include(c => c.Zone).FirstOrDefault(c => c.Zone.Id == itemId);

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
        public async Task<IActionResult> AddToCart(string itemId, [FromBody] AddToCartModel model)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimId == null)
            {
                return Unauthorized("Người dùng chưa đăng nhập");
            }

            var existingCartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == claimId && c.Zone.Id == itemId);

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

    }

    public class UpdateQuantityModel
    {
        public int Quantity { get; set; }
    }

    public class AddToCartModel
    {
        public int Quantity { get; set; }
    }


}