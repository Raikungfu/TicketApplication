using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    [AllowAnonymous]
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
            if (claimId == null) {
                return Unauthorized("Người dùng chưa đăng nhập"); 
            }

            var cart = await _context.Carts.Where(x => x.UserId == claimId).Include(x => x.Ticket).ToListAsync();

            return View(cart);
        }

        [HttpDelete]
        public IActionResult RemoveItem(string id)
        {
            var cartItem = _context.Carts.FirstOrDefault(c => c.Ticket.Id == id);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();

                return Ok();
            }

            return NotFound();
        }

        [HttpPut]
        public IActionResult UpdateQuantity(string itemId, [FromBody] UpdateQuantityModel model)
        {
            var cartItem = _context.Carts.FirstOrDefault(c => c.Ticket.Id == itemId);

            if (cartItem != null)
            {
                cartItem.Quantity = model.Quantity;
                _context.SaveChanges();
                return Ok();
            }

            return NotFound();
        }

    }

    public class UpdateQuantityModel
    {
        public int Quantity { get; set; }
    }

}
