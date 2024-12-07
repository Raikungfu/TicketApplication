using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        public IActionResult Profile()
        {
            var userId = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == userId);

            if (user == null)
            {
                return NotFound();
            }

            var rankLogoUrl = GetRankLogoUrl(user.Rank);
            ViewData["RankLogo"] = rankLogoUrl;

            return View(user);
        }

        public IActionResult EditProfile()
        {
            var userId = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == userId);

            if (user == null)
            {
                return NotFound();
            }

            var rankLogoUrl = GetRankLogoUrl(user.Rank);
            ViewData["RankLogo"] = rankLogoUrl;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(User user)
        {
            var userId = User.Identity.Name;
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == userId);
            if (existingUser != null)
            {
                if (!string.IsNullOrWhiteSpace(user.Name))
                {
                    existingUser.Name = user.Name;
                }

                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    existingUser.PhoneNumber = user.PhoneNumber;
                }

                if (!string.IsNullOrWhiteSpace(user.Address))
                {
                    existingUser.Address = user.Address;
                }

                _context.SaveChanges();
                return RedirectToAction(nameof(Profile));
            }
            return View(user);
        }

        private string GetRankLogoUrl(string rank)
        {
            return rank switch
            {
                "Gold" => "/Images/gold-logo.png",
                "Silver" => "/Images/silver-logo.png",
                "Bronze" => "/Images/bronze-logo.png",
                _ => "/Images/unknown-logo.png",
            };
        }

        // GET: Users/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Orders.Where(o => o.Status == "Paid"))
                    .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(oD => oD.Zone)
                    .ThenInclude(z => z.Event)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var purchasedEvents = user.Orders
     .SelectMany(o => o.OrderDetails)
     .GroupBy(oD => new { oD.Zone.Event.Title, oD.Zone.EventId })
     .Select(g => new
     {
         g.Key.Title,
         g.Key.EventId,
         TotalTickets = g.Sum(oD => oD.Quantity)
     })
     .ToList();

            ViewBag.PurchasedEvents = purchasedEvents;

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Name,Email,Password,PhoneNumber,Address,Role,Id,CreatedAt,CreatedBy,LastModified,LastModifiedBy")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Customer", Text = "Customer" },
                new SelectListItem { Value = "Admin", Text = "Admin" }
            };

            ViewBag.Roles = new SelectList(roles, "Value", "Text", user.Role);

            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, [Bind("Name,Email,Password,PhoneNumber,Address,Role,Id,CreatedAt,CreatedBy,LastModified,LastModifiedBy,Rank,IsBan")] User user)
        {
            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            if (id != user.Id || existingUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (existingUser.Password != user.Password)
                    {
                        var passwordHasher = new PasswordHasher<User>();
                        user.Password = passwordHasher.HashPassword(user, user.Password);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
            return View(user);
        }

        // GET: Users/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    var orderDetails = await _context.OrderDetails
                        .Where(od => od.OrderId == order.Id).Include(x => x.Tickets)
                        .ToListAsync();
                    foreach(var oD in orderDetails)
                    {
                        _context.Tickets.RemoveRange(oD.Tickets);
                    }
                    _context.OrderDetails.RemoveRange(orderDetails);
                    _context.Orders.Remove(order);
                }

                var cart = await _context.Carts.Where(x => x.UserId == user.Id).ToListAsync();
                _context.Carts.RemoveRange(cart);

                var rooms = await _context.Rooms
                    .Where(o => o.UserId == id).Include(x => x.Messages)
                    .ToListAsync();

                foreach (var r in rooms)
                {
                    _context.Messages.RemoveRange(r.Messages);
                    _context.Rooms.Remove(r);
                }

                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
