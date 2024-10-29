﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Tickets.Include(t => t.Zone);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                

                .Include(t => t.Zone)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            TempData["EventId"] = new SelectList(_context.Events, "Id", "Title");
            TempData["ZoneId"] = new SelectList(_context.Zones, "Id", "Name");
            var statuses = new List<SelectListItem>
    {
        new SelectListItem { Value = "Available", Text = "Available" },
        new SelectListItem { Value = "Unavailable", Text = "Unavailable" }
            };
            TempData["Status"] = statuses; // Pass statuses to the view
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,EventId,Status,ZoneId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.CreatedAt = DateTime.Now;

                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            //TempData["EventId"] = new SelectList(_context.Events, "Id", "Id", ticket.EventId);
            TempData["ZoneId"] = new SelectList(_context.Zones, "Id", "Id", ticket.ZoneId);
            
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.Include(t => t.Zone).FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }
            //TempData["EventId"] = new SelectList(_context.Events, "Id", "Title", ticket.Event.Title);
            TempData["ZoneId"] = new SelectList(_context.Zones, "Id", "Name", ticket.Zone.Name);
            var statuses = new List<SelectListItem>
    {
        new SelectListItem { Value = "Available", Text = "Available" },
        new SelectListItem { Value = "Unavailable", Text = "Unavailable" }
            };
            TempData["Status"] = statuses;
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Title,Description,EventId,Status,ZoneId,Id")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var ticketToEdit = _context.Tickets.Find(id);
                    ticketToEdit.Title = ticket.Title;
                    ticketToEdit.Description = ticket.Description;
                    //ticketToEdit.EventId = ticket.EventId;
                    ticketToEdit.Status = ticket.Status;
                    ticketToEdit.ZoneId = ticket.ZoneId;

                    _context.Entry(ticketToEdit).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
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
            //TempData["EventId"] = new SelectList(_context.Events, "Id", "Id", ticket.EventId);
            TempData["ZoneId"] = new SelectList(_context.Zones, "Id", "Id", ticket.ZoneId);
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Zone)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(string id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
}
