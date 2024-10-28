using Microsoft.AspNetCore.Mvc;

namespace TicketApplication.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
