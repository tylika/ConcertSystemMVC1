using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConcertSystemInfrastructure.Controllers
{
    public class ConcertsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public ConcertsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concert = await _context.Concerts
                .Include(c => c.Artist)
                .Include(c => c.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (concert == null)
            {
                return NotFound();
            }

            return View(concert);
        }

        // POST: Concerts/Delete/1
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var concert = await _context.Concerts
                    .Include(c => c.Tickets)
                    .Include(c => c.Genres)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (concert != null)
                {
                    _context.Concerts.Remove(concert);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Помилка при видаленні: {ex.Message}");
                var concert = await _context.Concerts.FindAsync(id);
                return View("Delete", concert);
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: Concerts
        public async Task<IActionResult> Index(string artistFilter, string genreFilter, string locationFilter, DateTime? dateFilter)
        {
            // Заповнюємо ViewBag для фільтрів
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
            ViewBag.GenreIds = new MultiSelectList(_context.Genres, "Id", "Name"); // Список жанрів для вибору
            return View();
        }

        // POST: Concerts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ArtistId,ConcertDate,Location,TotalTickets,AvailableTickets")] Concert concert, int[] GenreIds)
        {
            ModelState.Remove("Artist");
            ModelState.Remove("Location");
            if (ModelState.IsValid)
            {
                // Додаємо концерт до контексту
                _context.Add(concert);
                await _context.SaveChangesAsync(); // Зберігаємо концерт, щоб отримати Id

                // Додаємо вибрані жанри до концерту
                if (GenreIds != null && GenreIds.Length > 0)
                {
                    foreach (var genreId in GenreIds)
                    {
                        var genre = await _context.Genres.FindAsync(genreId);
                        if (genre != null)
                        {
                            concert.Genres.Add(genre);
                        }
                    }
                    await _context.SaveChangesAsync(); // Зберігаємо оновлення з жанрами
                }

                return RedirectToAction(nameof(Index));
            }

            // Якщо валідація не пройшла, повертаємо форму з даними
            ViewBag.ArtistId = new SelectList(_context.Artists, "Id", "FullName", concert.ArtistId);
            ViewBag.GenreIds = new MultiSelectList(_context.Genres, "Id", "Name", GenreIds);
            return View(concert);
        }
    }
}