using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;

namespace TicketApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Revenue([FromQuery] string? filter = "day", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;
                startDate = today.AddMonths(-12);
                endDate = today;
            }

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Filter = filter;
            
            return View();
        }

        public async Task<IActionResult> RevenueJson([FromQuery] string? filter = "day", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;
                startDate = new DateTime(today.Year, today.Month - 3, 1);
                endDate = today;
            }

            return Ok(await CalRevenue(filter, startDate, endDate));
        }


        public async Task<List<Object>> CalRevenue(string filter, DateTime? startDate, DateTime? endDate)
        {
            IQueryable<object> revenueQuery;

            switch (filter.ToLower())
            {
                case "month":
                    revenueQuery = _context.Orders
                        .Where(o => o.CreatedAt.HasValue && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                        .GroupBy(o => new { o.CreatedAt.Value.Year, o.CreatedAt.Value.Month })
                        .Select(g => new
                        {
                            Period = $"{g.Key.Month}/{g.Key.Year}",
                            TotalAmount = g.Sum(o => o.TotalAmount)
                        });
                    break;

                case "year":
                    revenueQuery = _context.Orders
                        .Where(o => o.CreatedAt.HasValue && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                        .GroupBy(o => o.CreatedAt.Value.Year)
                        .Select(g => new
                        {
                            Period = g.Key.ToString(),
                            TotalAmount = g.Sum(o => o.TotalAmount)
                        });
                    break;

                case "day":
                default:
                    revenueQuery = _context.Orders
                        .Where(o => o.CreatedAt.HasValue && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                        .GroupBy(o => o.CreatedAt.Value.Date)
                        .Select(g => new
                        {
                            Period = g.Key.ToString("dd/MM/yyyy"),
                            TotalAmount = g.Sum(o => o.TotalAmount)
                        });
                    break;
            }

            return await revenueQuery.ToListAsync();
        }
    }

}
