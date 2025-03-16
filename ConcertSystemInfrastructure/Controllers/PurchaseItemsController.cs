using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;

namespace ConcertSystemInfrastructure.Controllers
{
    public class PurchaseItemsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public PurchaseItemsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        // GET: PurchaseItems
        public async Task<IActionResult> Index()
        {
            var concertTicketSystemContext = _context.PurchaseItems.Include(p => p.Purchase).Include(p => p.Ticket);
            return View(await concertTicketSystemContext.ToListAsync());
        }

        // GET: PurchaseItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchaseItem = await _context.PurchaseItems
                .Include(p => p.Purchase)
                .Include(p => p.Ticket)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (purchaseItem == null)
            {
                return NotFound();
            }

            return View(purchaseItem);
        }

        // GET: PurchaseItems/Create
        public IActionResult Create()
        {
            ViewData["PurchaseId"] = new SelectList(_context.Purchases, "Id", "Status");
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Status");
            return View();
        }

        // POST: PurchaseItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PurchaseId,TicketId,Quantity,Price,Id")] PurchaseItem purchaseItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(purchaseItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PurchaseId"] = new SelectList(_context.Purchases, "Id", "Status", purchaseItem.PurchaseId);
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Status", purchaseItem.TicketId);
            return View(purchaseItem);
        }

        // GET: PurchaseItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchaseItem = await _context.PurchaseItems.FindAsync(id);
            if (purchaseItem == null)
            {
                return NotFound();
            }
            ViewData["PurchaseId"] = new SelectList(_context.Purchases, "Id", "Status", purchaseItem.PurchaseId);
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Status", purchaseItem.TicketId);
            return View(purchaseItem);
        }

        // POST: PurchaseItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PurchaseId,TicketId,Quantity,Price,Id")] PurchaseItem purchaseItem)
        {
            if (id != purchaseItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(purchaseItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PurchaseItemExists(purchaseItem.Id))
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
            ViewData["PurchaseId"] = new SelectList(_context.Purchases, "Id", "Status", purchaseItem.PurchaseId);
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Status", purchaseItem.TicketId);
            return View(purchaseItem);
        }

        // GET: PurchaseItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchaseItem = await _context.PurchaseItems
                .Include(p => p.Purchase)
                .Include(p => p.Ticket)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (purchaseItem == null)
            {
                return NotFound();
            }

            return View(purchaseItem);
        }

        // POST: PurchaseItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchaseItem = await _context.PurchaseItems.FindAsync(id);
            if (purchaseItem != null)
            {
                _context.PurchaseItems.Remove(purchaseItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PurchaseItemExists(int id)
        {
            return _context.PurchaseItems.Any(e => e.Id == id);
        }
    }
}
