using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace ConcertSystemInfrastructure.Controllers
{
    public class ArtistsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public ArtistsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        // GET: Artists
        public async Task<IActionResult> Index()
        {
            ViewData["Artists"] = await _context.Artists.Select(a => a.FullName).Distinct().ToListAsync();
            ViewData["Genres"] = await _context.Genres.Select(g => g.Name).Distinct().ToListAsync();
            ViewData["Locations"] = await _context.Concerts.Select(c => c.Location).Distinct().ToListAsync();

            return View(await _context.Artists.ToListAsync());
        }

        // GET: Artists/Details/5
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Artists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var artist = await _context.Artists.FindAsync(id);
            if (artist != null)
            {
                _context.Artists.Remove(artist);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ArtistExists(int id)
        {
            return _context.Artists.Any(e => e.Id == id);
        }
    }
}