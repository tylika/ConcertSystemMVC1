using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using Microsoft.AspNetCore.Authorization;

namespace ConcertSystemInfrastructure.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SpectatorsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public SpectatorsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Spectators.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Phone,Email")] Spectator spectator)
        {
            // Перевірка унікальності ПІБ
            if (await _context.Spectators.AnyAsync(s => s.FullName == spectator.FullName))
            {
                ModelState.AddModelError("FullName", "Глядач із таким ПІБ уже існує.");
            }

            // Перевірка унікальності телефону
            if (!string.IsNullOrEmpty(spectator.Phone) && await _context.Spectators.AnyAsync(s => s.Phone == spectator.Phone))
            {
                ModelState.AddModelError("Phone", "Глядач із таким номером телефону уже існує.");
            }

            // Перевірка унікальності email
            if (await _context.Spectators.AnyAsync(s => s.Email == spectator.Email))
            {
                ModelState.AddModelError("Email", "Глядач із такою електронною поштою уже існує.");
            }

            // Кастомна валідація номера телефону
            if (!string.IsNullOrEmpty(spectator.Phone) && (spectator.Phone.Length != 13 || !spectator.Phone.StartsWith("+") || !spectator.Phone.Substring(1).All(char.IsDigit)))
            {
                ModelState.AddModelError("Phone", "Номер телефону має бути у форматі +380XXXXXXXXX (13 символів, лише цифри після +).");
            }

            if (ModelState.IsValid)
            {
                _context.Add(spectator);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(spectator);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var spectator = await _context.Spectators.FindAsync(id);
            if (spectator == null) return NotFound();
            return View(spectator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Phone,Email")] Spectator spectator)
        {
            if (id != spectator.Id) return NotFound();

            // Перевірка унікальності ПІБ (крім поточного запису)
            if (await _context.Spectators.AnyAsync(s => s.FullName == spectator.FullName && s.Id != spectator.Id))
            {
                ModelState.AddModelError("FullName", "Глядач із таким ПІБ уже існує.");
            }

            // Перевірка унікальності телефону (крім поточного запису)
            if (!string.IsNullOrEmpty(spectator.Phone) && await _context.Spectators.AnyAsync(s => s.Phone == spectator.Phone && s.Id != spectator.Id))
            {
                ModelState.AddModelError("Phone", "Глядач із таким номером телефону уже існує.");
            }

            // Перевірка унікальності email (крім поточного запису)
            if (await _context.Spectators.AnyAsync(s => s.Email == spectator.Email && s.Id != spectator.Id))
            {
                ModelState.AddModelError("Email", "Глядач із такою електронною поштою уже існує.");
            }

            // Кастомна валідація номера телефону
            if (!string.IsNullOrEmpty(spectator.Phone) && (spectator.Phone.Length != 13 || !spectator.Phone.StartsWith("+") || !spectator.Phone.Substring(1).All(char.IsDigit)))
            {
                ModelState.AddModelError("Phone", "Номер телефону має бути у форматі +380XXXXXXXXX (13 символів, лише цифри після +).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(spectator);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpectatorExists(spectator.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(spectator);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var spectator = await _context.Spectators.FirstOrDefaultAsync(m => m.Id == id);
            if (spectator == null) return NotFound();

            return View(spectator);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var spectator = await _context.Spectators.FindAsync(id);
            if (spectator != null)
            {
                _context.Spectators.Remove(spectator);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SpectatorExists(int id)
        {
            return _context.Spectators.Any(e => e.Id == id);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var spectator = await _context.Spectators
                .Include(s => s.Purchases)
                .ThenInclude(p => p.PurchaseItems)
                .ThenInclude(pi => pi.Ticket)
                .ThenInclude(t => t.Concert)
                .ThenInclude(c => c.Artist)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (spectator == null)
            {
                return NotFound();
            }

            return View(spectator);
        }
    }
}