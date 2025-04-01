using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ConcertSystemInfrastructure.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArtistsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public ArtistsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        // GET: Artists
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewData["Artists"] = await _context.Artists.Select(a => a.FullName).Distinct().ToListAsync();
            ViewData["Genres"] = await _context.Genres.Select(g => g.Name).Distinct().ToListAsync();
            ViewData["Locations"] = await _context.Concerts.Select(c => c.Location).Distinct().ToListAsync();

            return View(await _context.Artists.ToListAsync());
        }
        // GET: Artists/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists
                .Include(a => a.Concerts)
                .ThenInclude(c => c.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (artist == null)
            {
                return NotFound();
            }

            return View(artist);
        }

        // GET: Artists/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Artists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,FullName,SocialMedia")] Artist artist)
        {
            if (await _context.Artists.AnyAsync(a => a.FullName == artist.FullName))
            {
                ModelState.AddModelError("FullName", "Артист із такою назвою вже існує.");
            }

            if (await _context.Artists.AnyAsync(a => a.SocialMedia == artist.SocialMedia))
            {
                ModelState.AddModelError("SocialMedia", "Артист із такою соціальною мережею вже існує.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(artist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(artist);
        }

        // GET: Artists/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists.FindAsync(id);
            if (artist == null)
            {
                return NotFound();
            }
            return View(artist);
        }

        // POST: Artists/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,SocialMedia")] Artist artist)
        {
            if (id != artist.Id)
            {
                return NotFound();
            }

            if (await _context.Artists.AnyAsync(a => a.FullName == artist.FullName && a.Id != artist.Id))
            {
                ModelState.AddModelError("FullName", "Артист із такою назвою вже існує.");
            }

            if (await _context.Artists.AnyAsync(a => a.SocialMedia == artist.SocialMedia && a.Id != artist.Id))
            {
                ModelState.AddModelError("SocialMedia", "Артист із такою соціальною мережею вже існує.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(artist);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArtistExists(artist.Id))
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
            return View(artist);
        }

        // GET: Artists/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists
                .Include(a => a.Concerts)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (artist == null)
            {
                return NotFound();
            }

            return View(artist);
        }
        // POST: Artists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Завантажуємо артиста з пов'язаними концертами і квитками
                var artist = await _context.Artists
                    .Include(a => a.Concerts)
                        .ThenInclude(c => c.Tickets)
                            .ThenInclude(t => t.PurchaseItems)
                    .Include(a => a.Concerts)
                        .ThenInclude(c => c.Genres)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (artist == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Перевіряємо, чи є продані квитки
                bool hasSoldTickets = artist.Concerts
                    .Any(c => c.Tickets.Any(t => t.Status == "Sold"));

                if (hasSoldTickets)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Неможливо видалити артиста, оскільки на його концерти вже продані квитки.";
                    return RedirectToAction(nameof(Index));
                }

                // Видаляємо пов'язані концерти
                foreach (var concert in artist.Concerts)
                {
                    // Видаляємо PurchaseItems
                    foreach (var ticket in concert.Tickets)
                    {
                        _context.PurchaseItems.RemoveRange(ticket.PurchaseItems);
                    }
                    // Видаляємо квитки
                    _context.Tickets.RemoveRange(concert.Tickets);
                    // Очищаємо жанри
                    concert.Genres.Clear();
                    // Видаляємо концерт
                    _context.Concerts.Remove(concert);
                }

                // Видаляємо артиста
                _context.Artists.Remove(artist);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Артиста та його концерти успішно видалено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Помилка при видаленні артиста: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        private bool ArtistExists(int id)
        {
            return _context.Artists.Any(e => e.Id == id);
        }
    }
}