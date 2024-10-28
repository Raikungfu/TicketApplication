using Microsoft.AspNetCore.Mvc;

namespace TicketApplication.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
