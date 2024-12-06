using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Where(e => e.Date >= DateTime.Now && e.Status == "Visible").Include(e => e.Zones).AsNoTracking().ToListAsync();

            return View(events);
        }

        [HttpPost]
        public async Task<IActionResult> Filter([FromForm]SearchForm searchForm)
        {
            string categoryFilter;
            switch(searchForm.Category)
            {
                case "music":
                    categoryFilter = "âm nhạc";
                    break;
                case "sport":
                    categoryFilter = "thể thao";
                    break;
                case "theater":
                    categoryFilter = "hài kịch";
                    break;
                case "bartending":
                    categoryFilter = "pha chế";
                    break;
                case "academic":
                    categoryFilter = "học thuật";
                    break;
                case "all":
                    categoryFilter = "all";
                    break;
                default:
                    categoryFilter = "all";
                    break;
            }

            var events = await _context.Events
                .Where(e => e.Date <= DateTime.Now && e.Status == "Visible" &&
                    (categoryFilter == "all" || e.Title.ToLower().Contains(categoryFilter)) &&
                    e.Zones.Any(z => z.Price >= searchForm.PriceFrom && z.Price <= searchForm.PriceTo))
                .Include(e => e.Zones)
                .AsNoTracking()
                .Select(e => new
                {
                    e.Image,
                    e.Location,
                    e.MaxTicketPrice,
                    e.MinTicketPrice,
                    e.Date,
                    e.Status,
                    e.Title,
                    e.Id,
                    e.ImageFile
                })
                .ToListAsync();

            if (events.Count > 0)
            {
                return Ok(new { success = true, events });
            }
            return Ok(new { success = false, message = "No events found" });
        }

        public async Task<IActionResult> EventDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var eventDetail = await _context.Events.Where(e => e.Date >= DateTime.Now && e.Status == "Visible")
                .Include(e => e.Zones)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventDetail == null)
            {
                return NotFound();
            }

            ViewBag.FormattedDate = eventDetail.Date.ToString("dddd, HH:mm - dd/MM/yyyy");

            return View("EventDetail", eventDetail);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class SearchForm()
    {
        public int PriceFrom { get; set; }
        public int PriceTo { get; set; }
        public string Category { get; set; }
    }
}
