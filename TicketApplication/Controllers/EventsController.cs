using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Models;
using TicketApplication.Service;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using static System.Net.WebRequestMethods;

namespace TicketApplication.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UploadFileService _uploadFileService;
        private readonly EmailService _emailService;

        public EventsController(ApplicationDbContext context, UploadFileService uploadFileService, EmailService emailService)
        {
            _context = context;
            _uploadFileService = uploadFileService;
            _emailService = emailService;
        }

        // GET: Events
        public async Task<IActionResult> Index(string? filter)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role == "Customer")
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEvents = await _context.Events.Where(e => e.CreatedBy.Equals(userName)).Include(e => e.Zones).ToListAsync();
                return View(userEvents);
            }
            else
            {
                var events = _context.Events.Include(e => e.Zones).AsQueryable();

                if (!string.IsNullOrEmpty(filter))
                {
                    switch (filter)
                    {
                        case "Admin":
                            events = events.Where(e => e.CreatedBy == "Admin");
                            break;
                        case "Customer":
                            events = events.Where(e => e.CreatedBy != "Admin");
                            break;
                        default:
                            break;
                    }
                }

                return View(await events.ToListAsync());
            }
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.Include(e => e.Zones)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Customer" && @event.CreatedBy != User.Identity.Name)
            {
                return Forbid();
            }

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Location,Date,ImageFile")] Event @event)
        {
            if (ModelState.IsValid)
            {

                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userName.IsNullOrEmpty())
                {
                    return Forbid();
                }

                @event.CreatedBy = userName;

                var role = User.FindFirstValue(ClaimTypes.Role);
                if (role == "Customer")
                {
                    @event.Status = "Pending";
                }
                else if(role == "Admin")
                {
                    @event.Status = "Visible";
                    @event.CreatedBy = "Admin";
                }
                else
                {
                    @event.Status = "Visible";
                }

                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (@event.ImageFile != null)
                {
                    @event.Image = _uploadFileService.uploadImage(@event.ImageFile, "Images");
                }
                @event.CreatedAt = DateTime.Now;
                _context.Add(@event);
                await _context.SaveChangesAsync();

                if (role == "Customer")
                {
                    TempData["ShowModal"] = true;
                }

                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (role == "Customer" && !@event.CreatedBy.Equals(userName))
            {
                return Forbid();
            }

            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Title,Description,Location,Date,ImageFile,Id,Status")] Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    var eventToUpdate = await _context.Events.FindAsync(id);

                    var role = User.FindFirstValue(ClaimTypes.Role);
                    var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (role == "Customer" && !eventToUpdate.CreatedBy.Equals(userName))
                    {
                        return Forbid();
                    }

                    if (role == "Admin")
                    {
                        if(!eventToUpdate.CreatedBy.Equals("Admin") && @event.Status.Equals("Rejected") && eventToUpdate.Status.Equals("Pending"))
                        {
                            var cusMail = _context.Users.FirstOrDefault(u => u.Email.Equals(@event.CreatedBy)).Email;
                            _emailService.SendMail(
                                title: $"WorQshop. Status Workshop {@event.Title}",
                                recip: cusMail,
                                body: $"Chúng tôi rất tiếc phải thông báo rằng workshop <b>{@event.Title}</b> của bạn chưa được duyệt. Bạn có thể xem xét chỉnh sửa và gửi lại yêu cầu."
                            );
                        }

                        eventToUpdate.Status = @event.Status;
                    }

                    eventToUpdate.Title = @event.Title;
                    eventToUpdate.Description = @event.Description;
                    eventToUpdate.Location = @event.Location;
                    eventToUpdate.Date = @event.Date;

                    if (@event.ImageFile != null)
                    {
                        eventToUpdate.Image = _uploadFileService.uploadImage(@event.ImageFile, "Images");
                    }

                    _context.Entry(eventToUpdate).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
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
            return View(@event);
        }


        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (role == "Customer" && !@event.CreatedBy.Equals(userName))
            {
                return Forbid();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (role == "Customer" && !@event.CreatedBy.Equals(userName))
                {
                    return Forbid();
                }

                _context.Events.Remove(@event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(string id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
