using Microsoft.AspNetCore.Mvc;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    public class CartController : Controller
    {

        public CartController()
        {
        }

        public IActionResult Index()
        {
            var cart = new List<CartItem>
            {
                new CartItem { Name = "JSOL Fan Meeting 2024 – FM CHÁY SON", Price = 888000, Quantity = 1, ImageUrl = "/path/to/image.jpg" }
            };

            return View(cart);
        }
    }
}
