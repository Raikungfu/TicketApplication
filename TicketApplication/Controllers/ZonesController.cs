using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Models;
using System.Security.Claims;

namespace TicketApplication.Controllers
{
    public class ZonesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ZonesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Zones
        public async Task<IActionResult> Index()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Customer")
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userZones = await _context.Zones
                    .Where(z => z.Event.CreatedBy.Equals(userName))
                    .Include(z => z.Event)
                    .AsNoTracking()
                    .ToListAsync();
                return View(userZones);
            }
            else
            {
                return View(await _context.Zones.Include(z => z.Event).AsNoTracking().ToListAsync());
            }
        }

        // GET: Zones/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var zone = await _context.Zones
                .Include(z => z.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (zone == null)
            {
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (role == "Customer" && !zone.Event.CreatedBy.Equals(userName))
            {
                return Forbid();
            }

            return View(zone);
        }

        // GET: Zones/Create
        public IActionResult Create()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Customer")
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEvents = _context.Events.Where(e => e.CreatedBy == userName).ToList();
                ViewBag.EventId = new SelectList(userEvents, "Id", "Title");
            }
            else
            {
                ViewBag.EventId = new SelectList(_context.Events, "Id", "Title");
            }

            return View();
        }

        // POST: Zones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,AvailableTickets,Description,EventId")] Zone zone)
        {
            if (ModelState.IsValid)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                if (role == "Customer")
                {
                    var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var eventCreatedBy = _context.Events.Where(e => e.Id == zone.EventId).FirstOrDefault()?.CreatedBy;
                    if (!eventCreatedBy.Equals(userName))
                    {
                        return Forbid();
                    }
                }

                zone.CreatedAt = DateTime.Now;
                _context.Add(zone);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(zone);
        }

        // GET: Zones/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var zone = await _context.Zones
                .Include(z => z.Event)
                .FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null)
            {
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (role == "Customer" && !zone.Event.CreatedBy.Equals(userName))
            {
                return Forbid();
            }

            if (role == "Customer")
            {
                TempData["ShowModal"] = true;
            }

            ViewBag.EventId = new SelectList(_context.Events, "Id", "Title");
            return View(zone);
        }

        // POST: Zones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Name,Price,AvailableTickets,Description,EventId,Id")] Zone zone)
        {
            if (id != zone.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var zoneToEdit = _context.Zones.Find(id);

                    var role = User.FindFirstValue(ClaimTypes.Role);
                    var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (role == "Customer" && !zoneToEdit.Event.CreatedBy.Equals(userName))
                    {
                        return Forbid();
                    }

                    zoneToEdit.Name = zone.Name;
                    zoneToEdit.Price = zone.Price;
                    zoneToEdit.AvailableTickets = zone.AvailableTickets;
                    zoneToEdit.Description = zone.Description;
                    zoneToEdit.EventId = zone.EventId;

                    _context.Entry(zoneToEdit).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ZoneExists(zone.Id))
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
            return View(zone);
        }

        // GET: Zones/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var zone = await _context.Zones
                .Include(z => z.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (zone == null)
            {
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (role == "Customer" && !zone.Event.CreatedBy.Equals(userName))
            {
                return Forbid();
            }

            return View(zone);
        }

        // POST: Zones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var zone = await _context.Zones.Include(z => z.Tickets).FirstOrDefaultAsync(z => z.Id == id);
            if (zone != null)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (role == "Customer" && !zone.Event.CreatedBy.Equals(userName))
                {
                    return Forbid();
                }

                _context.Tickets.RemoveRange(zone.Tickets);

                _context.Zones.Remove(zone);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ZoneExists(string id)
        {
            return _context.Zones.Any(e => e.Id == id);
        }
    }
}
