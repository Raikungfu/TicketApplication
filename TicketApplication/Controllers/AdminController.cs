using Microsoft.AspNetCore.Mvc;

namespace TicketApplication.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
