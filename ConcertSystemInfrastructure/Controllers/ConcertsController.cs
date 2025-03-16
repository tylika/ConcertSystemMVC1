using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConcertSystemInfrastructure.Controllers
{
    public class ConcertsController : Controller // Переконуємося, що успадковується від Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public ConcertsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        // GET: Concerts
        public async Task<IActionResult> Index(string artistFilter, string genreFilter, string locationFilter, DateTime? dateFilter)
        {
            ViewBag.Artists = await _context.Artists.Select(a => a.FullName).Distinct().ToListAsync();
            ViewBag.Genres = await _context.Genres.Select(g => g.Name).Distinct().ToListAsync();
            ViewBag.Locations = await _context.Concerts.Select(c => c.Location).Distinct().ToListAsync();

            var concerts = _context.Concerts
                .Include(c => c.Artist)
                .Include(c => c.Genres)
                .AsQueryable();

            if (!string.IsNullOrEmpty(artistFilter))
            {
                concerts = concerts.Where(c => c.Artist.FullName.Contains(artistFilter));
            }

            if (!string.IsNullOrEmpty(genreFilter))
            {
                concerts = concerts.Where(c => c.Genres.Any(g => g.Name == genreFilter));
            }

            if (!string.IsNullOrEmpty(locationFilter))
            {
                concerts = concerts.Where(c => c.Location.Contains(locationFilter));
            }

            if (dateFilter.HasValue)
            {
                concerts = concerts.Where(c => c.ConcertDate.Date == dateFilter.Value.Date);
            }

            return View(await concerts.ToListAsync());
        }

        // GET: Concerts/Create
        public IActionResult Create()
        {
            ViewBag.ArtistId = new SelectList(_context.Artists, "Id", "FullName");
            ViewBag.GenreIds = new SelectList(_context.Genres, "Id", "Name");
            return View();
        }

        // POST: Concerts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ArtistId,ConcertDate,Location,TotalTickets,AvailableTickets")] Concert concert, int[] GenreIds)
        {
            // Перевірка дати
            DateTime minAllowedDate = DateTime.Now.AddMonths(1);
            if (concert.ConcertDate < minAllowedDate)
            {
                ModelState.AddModelError("ConcertDate", "Концерт має бути запланований щонайменше за місяць від поточної дати");
            }

            // Перевірка кількості квитків
            if (concert.AvailableTickets > concert.TotalTickets)
            {
                ModelState.AddModelError("AvailableTickets", "Доступних квитків не може бути більше, ніж загальна кількість квитків");
            }
            ModelState.Remove("Artist");
            ModelState.Remove("Location");
            if (ModelState.IsValid)
            {
                _context.Add(concert);
                await _context.SaveChangesAsync();

                if (GenreIds != null)
                {
                    concert.Genres = await _context.Genres.Where(g => GenreIds.Contains(g.Id)).ToListAsync();
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ArtistId = new SelectList(await _context.Artists.Select(a => new { a.Id, a.FullName }).ToListAsync(), "Id", "FullName");
            ViewBag.GenreIds = new SelectList(await _context.Genres.Select(g => new { g.Id, g.Name }).ToListAsync(), "Id", "Name");
            return View(concert);
        }

        // GET: Concerts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null)
            {
                return NotFound();
            }

            ViewBag.ArtistId = new SelectList(await _context.Artists.Select(a => new { a.Id, a.FullName }).ToListAsync(), "Id", "FullName", concert.ArtistId);
            ViewBag.GenreIds = new SelectList(await _context.Genres.Select(g => new { g.Id, g.Name }).ToListAsync(), "Id", "Name");
            return View(concert);
        }

        // POST: Concerts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ArtistId,ConcertDate,Location,TotalTickets,AvailableTickets")] Concert concert, int[] GenreIds)
        {
            if (id != concert.Id)
            {
                return NotFound();
            }

            // Перевірка дати
            DateTime minAllowedDate = DateTime.Now.AddMonths(1);
            if (concert.ConcertDate < minAllowedDate)
            {
                ModelState.AddModelError("ConcertDate", "Концерт має бути запланований щонайменше за місяць від поточної дати");
            }

            // Перевірка кількості квитків
            if (concert.AvailableTickets > concert.TotalTickets)
            {
                ModelState.AddModelError("AvailableTickets", "Доступних квитків не може бути більше, ніж загальна кількість квитків");
            }
            ModelState.Remove("Artist");
            ModelState.Remove("Location");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(concert);
                    await _context.SaveChangesAsync();

                    var existingGenres = await _context.Concerts
                        .Include(c => c.Genres)
                        .FirstOrDefaultAsync(c => c.Id == concert.Id);
                    if (existingGenres != null)
                    {
                        existingGenres.Genres.Clear();
                        if (GenreIds != null)
                        {
                            var newGenres = await _context.Genres.Where(g => GenreIds.Contains(g.Id)).ToListAsync();
                            foreach (var genre in newGenres)
                            {
                                existingGenres.Genres.Add(genre);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConcertExists(concert.Id))
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

            ViewBag.ArtistId = new SelectList(await _context.Artists.Select(a => new { a.Id, a.FullName }).ToListAsync(), "Id", "FullName", concert.ArtistId);
            ViewBag.GenreIds = new SelectList(await _context.Genres.Select(g => new { g.Id, g.Name }).ToListAsync(), "Id", "Name");
            return View(concert);
        }

        // GET: Concerts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concert = await _context.Concerts
                .Include(c => c.Artist)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (concert == null)
            {
                return NotFound();
            }

            return View(concert);
        }

        // POST: Concerts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert != null)
            {
                _context.Concerts.Remove(concert);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Concerts/BuyTicket/5
        public async Task<IActionResult> BuyTicket(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concert = await _context.Concerts
                .Include(c => c.Artist)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (concert == null)
            {
                return NotFound();
            }

            if (concert.AvailableTickets <= 0)
            {
                TempData["ErrorMessage"] = "На жаль, квитки на цей концерт закінчилися.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Concert = concert;
            return View(new Ticket { ConcertId = concert.Id });
        }

        // POST: Concerts/BuyTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyTicket(int id, [Bind("Id,ConcertId,BuyerName")] Ticket ticket)
        {
            if (id != ticket.ConcertId)
            {
                return NotFound();
            }

            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null)
            {
                return NotFound();
            }

            if (concert.AvailableTickets <= 0)
            {
                TempData["ErrorMessage"] = "На жаль, квитки на цей концерт закінчилися.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.Remove("Artist");
            ModelState.Remove("Location");
            if (ModelState.IsValid)
            {
                _context.Add(ticket);
                concert.AvailableTickets--;
                _context.Update(concert);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Квиток успішно куплено!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Concert = concert;
            return View(ticket);
        }

        private bool ConcertExists(int id)
        {
            return _context.Concerts.Any(e => e.Id == id);
        }
    }
}