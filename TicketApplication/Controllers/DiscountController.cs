using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Models;
using System.Linq;
using System.Threading.Tasks;
using TicketApplication.Data;
using Microsoft.AspNetCore.Authorization;

namespace TicketApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DiscountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var discounts = await _context.Discounts.ToListAsync();
            return View(discounts);
        }

        // Trang tạo mới Discount
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Description,DiscountPercentage,DiscountAmount,ValidFrom,ValidUntil,UsageLimit,IsActive,CreatedBy,LastModifiedBy")] Discount discount)
        {
            if (ModelState.IsValid)
            {
                discount.CreatedAt = DateTime.Now;
                discount.CreatedBy = "Admin";
                _context.Add(discount);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(discount);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }
            return View(discount);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Code,Description,DiscountPercentage,DiscountAmount,ValidFrom,ValidUntil,UsageLimit,IsActive,CreatedAt,CreatedBy,LastModifiedBy")] Discount discount)
        {
            if (id != discount.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    discount.LastModified = DateTime.Now;
                    _context.Update(discount);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountExists(discount.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(discount);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (discount == null)
            {
                return NotFound();
            }

            return View(discount);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DiscountExists(string id)
        {
            return _context.Discounts.Any(e => e.Id == id);
        }
    }
}
