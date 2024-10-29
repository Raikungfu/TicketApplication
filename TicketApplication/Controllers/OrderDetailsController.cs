using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Helper;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OrderDetails
        public async Task<IActionResult> Index(
     string? searchTemp,
     int pageNumber = 1,
     int pageSize = 10   )
        {
            IQueryable<OrderDetail> applicationDbContext = _context.OrderDetails
                .Include(o => o.Order).ThenInclude(u => u.User)
                .Include(o => o.Ticket).ThenInclude(s => s.Zone).ThenInclude(t => t.Event);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTemp))
            {
                applicationDbContext = applicationDbContext.Where(s =>
                    s.TicketId.ToString().Contains(searchTemp) ||
                    s.OrderId.ToString().Contains(searchTemp) ||
                    s.Order.User.Email.Contains(searchTemp) ||
                    s.Ticket.Zone.Event.Title.Contains(searchTemp));
            }

         

            var paginatedList = await PaginatedList<OrderDetail>.CreateAsync(applicationDbContext, pageNumber, pageSize);

            // Pass pagination and sorting data to the view
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = paginatedList.TotalPages;
            ViewBag.SearchTemp = searchTemp;
           

            return View(paginatedList);
        }


        // GET: OrderDetails/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails
                .Include(o => o.Order)
                .Include(o => o.Ticket)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            return View(orderDetail);
        }

        // GET: OrderDetails/Create
        public IActionResult Create()
        {
            TempData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");
            TempData["TicketId"] = new SelectList(_context.Tickets, "Id", "Id");
            return View();
        }

        // POST: OrderDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,TicketId,Quantity,UnitPrice,TotalPrice")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orderDetail);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            TempData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderDetail.OrderId);
            TempData["TicketId"] = new SelectList(_context.Tickets, "Id", "Id", orderDetail.TicketId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails.FindAsync(id);
            if (orderDetail == null)
            {
                return NotFound();
            }
            TempData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderDetail.OrderId);
            TempData["TicketId"] = new SelectList(_context.Tickets, "Id", "Id", orderDetail.TicketId);
            return View(orderDetail);
        }

        // POST: OrderDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("OrderId,TicketId,Quantity,UnitPrice,TotalPrice")] OrderDetail orderDetail)
        {
            if (id != orderDetail.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderDetail);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderDetailExists(orderDetail.OrderId))
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
            TempData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderDetail.OrderId);
            TempData["TicketId"] = new SelectList(_context.Tickets, "Id", "Id", orderDetail.TicketId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails
                .Include(o => o.Order)
                .Include(o => o.Ticket)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            return View(orderDetail);
        }

        // POST: OrderDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var orderDetail = await _context.OrderDetails.FindAsync(id);
            if (orderDetail != null)
            {
                _context.OrderDetails.Remove(orderDetail);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderDetailExists(string id)
        {
            return _context.OrderDetails.Any(e => e.OrderId == id);
        }
    }
}
