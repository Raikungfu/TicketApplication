using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Models;
using TicketApplication.Service;

namespace TicketApplication.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UploadFileService _uploadFileService;

        public EventsController(ApplicationDbContext context, UploadFileService uploadFileService)
        {
            _context = context;
            _uploadFileService = uploadFileService;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            return View(await _context.Events.Include(e => e.Zones).ToListAsync());
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

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Location,Date,ImageFile")] Event @event)
        {
            if (ModelState.IsValid)
            {
                // Define the directory path
                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");

                // Check if the directory exists, if not, create it
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
            if (!string.IsNullOrEmpty(@event.Date))
            {
                // Assuming the original date is in "MM/dd/yyyy HH:mm" format or adjust as needed
                DateTime parsedDate;
                if (DateTime.TryParse(@event.Date, out parsedDate))
                {
                    @event.Date = parsedDate.ToString("yyyy-MM-ddTHH:mm"); // Format for datetime-local
                }
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Title,Description,Location,Date,ImageFile,Id")] Event @event)
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
                    eventToUpdate.Title = @event.Title;
                    eventToUpdate.Description = @event.Description;
                    eventToUpdate.Location = @event.Location;
                    eventToUpdate.Date = @event.Date;
                    eventToUpdate.ImageFile =  @event.ImageFile;
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
