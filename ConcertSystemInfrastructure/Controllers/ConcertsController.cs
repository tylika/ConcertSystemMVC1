using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ConcertSystemInfrastructure.Controllers
{
    
    public class ConcertsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConcertsController(ConcertTicketSystemContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Concerts
        [AllowAnonymous]
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
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.ArtistId = new SelectList(_context.Artists, "Id", "FullName");
            ViewBag.GenreIds = new SelectList(_context.Genres, "Id", "Name");
            return View();
        }

        // POST: Concerts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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
                // Додаємо концерт
                _context.Add(concert);
                await _context.SaveChangesAsync();

                // Створюємо квитки
                for (int i = 1; i <= concert.TotalTickets; i++)
                {
                    var ticket = new Ticket
                    {
                        ConcertId = concert.Id,
                        Row = "A",
                        SeatNumber = i,
                        BasePrice = 100,
                        Status = "Available"
                    };
                    _context.Tickets.Add(ticket);
                }
                await _context.SaveChangesAsync();

                // Додаємо жанри
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
                    // Отримуємо оригінальний концерт
                    var originalConcert = await _context.Concerts
                        .Include(c => c.Tickets)
                        .FirstOrDefaultAsync(c => c.Id == concert.Id);

                    if (originalConcert == null)
                    {
                        return NotFound();
                    }

                    // Оновлюємо поля концерту
                    _context.Entry(originalConcert).CurrentValues.SetValues(concert);

                    // Синхронізуємо квитки
                    int currentTicketCount = originalConcert.Tickets.Count;
                    if (concert.TotalTickets > currentTicketCount)
                    {
                        // Додаємо нові квитки
                        for (int i = currentTicketCount + 1; i <= concert.TotalTickets; i++)
                        {
                            var ticket = new Ticket
                            {
                                ConcertId = concert.Id,
                                Row = "A",
                                SeatNumber = i,
                                BasePrice = 100,
                                Status = "Available"
                            };
                            _context.Tickets.Add(ticket);
                        }
                    }
                    else if (concert.TotalTickets < currentTicketCount)
                    {
                        // Видаляємо зайві квитки (тільки ті, що не продані)
                        var ticketsToRemove = originalConcert.Tickets
                            .Where(t => t.Status == "Available")
                            .OrderByDescending(t => t.SeatNumber)
                            .Take(currentTicketCount - concert.TotalTickets)
                            .ToList();

                        foreach (var ticket in ticketsToRemove)
                        {
                            _context.Tickets.Remove(ticket);
                        }
                    }

                    // Перевіряємо, чи AvailableTickets не перевищує нову кількість TotalTickets
                    if (concert.AvailableTickets > concert.TotalTickets)
                    {
                        concert.AvailableTickets = concert.TotalTickets;
                    }

                    await _context.SaveChangesAsync();

                    // Оновлюємо жанри
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
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var concert = await _context.Concerts
                .Include(c => c.Artist) // Включаємо дані артиста
                .Include(c => c.Genres) // Включаємо жанри концерту
                .Include(c => c.Tickets) // Включаємо квитки (опціонально)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (concert == null)
            {
                return NotFound();
            }

            return View(concert);
        }
        // GET: Concerts/Delete/5
        [Authorize(Roles = "Admin")]
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Завантажуємо концерт разом із пов'язаними сутностями
                var concert = await _context.Concerts
                    .Include(c => c.Tickets)
                        .ThenInclude(t => t.PurchaseItems)
                    .Include(c => c.Genres)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (concert == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                // Перевіряємо, чи є продані квитки
                if (concert.Tickets.Any(t => t.Status == "Sold"))
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Неможливо видалити концерт, оскільки на нього вже продані квитки.";
                    return RedirectToAction(nameof(Index));
                }

                // Видаляємо пов'язані PurchaseItems
                foreach (var ticket in concert.Tickets)
                {
                    _context.PurchaseItems.RemoveRange(ticket.PurchaseItems);
                }

                // Видаляємо квитки
                _context.Tickets.RemoveRange(concert.Tickets);

                // Очищаємо зв'язок з жанрами
                concert.Genres.Clear();

                // Видаляємо сам концерт
                _context.Concerts.Remove(concert);

                // Зберігаємо зміни
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Концерт успішно видалено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Помилка при видаленні концерту: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: Concerts/BuyTicket/5
        // GET: Concerts/BuyTicket/5
        [Authorize(Roles = "Viewer")]
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

            // Передаємо концерт у ViewBag для відображення інформації
            ViewBag.Concert = concert;
            // Передаємо список доступних квитків для концерту
            ViewBag.Tickets = await _context.Tickets
                .Where(t => t.ConcertId == concert.Id && t.Status == "Available")
                .ToListAsync();

            // Повертаємо форму для введення даних глядача
            return View(new Spectator());
        }
        // POST: Concerts/BuyTicket/5
        // POST: Concerts/BuyTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Viewer")]
        public async Task<IActionResult> BuyTicket(int id, string phone, int? ticketId)
        {
            // Отримуємо концерт з бази даних разом із квитками
            var concert = await _context.Concerts
                .Include(c => c.Tickets)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (concert == null)
            {
                return NotFound();
            }

            // Перевіряємо, чи є доступні квитки
            if (concert.AvailableTickets <= 0)
            {
                TempData["ErrorMessage"] = "На жаль, квитки на цей концерт закінчилися.";
                return RedirectToAction(nameof(Index));
            }

            // Отримуємо дані автентифікованого користувача
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Користувач не автентифікований.";
                return RedirectToAction(nameof(Index));
            }

            // Створюємо об'єкт Spectator із даними користувача
            var spectator = new Spectator
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = phone
            };

            // Починаємо транзакцію для забезпечення цілісності даних
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Перевіряємо, чи глядач уже існує за email
                var existingSpectator = await _context.Spectators
                    .FirstOrDefaultAsync(s => s.Email == spectator.Email);
                if (existingSpectator == null)
                {
                    // Перевіряємо валідність номера телефону
                    if (string.IsNullOrEmpty(phone) || phone.Length != 13 || !phone.StartsWith("+") || !phone.Substring(1).All(char.IsDigit))
                    {
                        TempData["ErrorMessage"] = "Номер телефону має бути у форматі +380XXXXXXXXX (13 символів, лише цифри після +).";
                        ViewBag.Concert = concert;
                        ViewBag.Tickets = await _context.Tickets
                            .Where(t => t.ConcertId == concert.Id && t.Status == "Available")
                            .ToListAsync();
                        return View(spectator);
                    }

                    // Якщо глядача немає, додаємо нового
                    _context.Spectators.Add(spectator);
                    await _context.SaveChangesAsync();
                    existingSpectator = spectator;
                }
                else
                {
                    // Оновлюємо номер телефону, якщо глядач уже існує
                    existingSpectator.Phone = phone;
                    _context.Update(existingSpectator);
                    await _context.SaveChangesAsync();
                }

                // Вибираємо квиток: або за ticketId, або перший доступний
                var ticket = ticketId.HasValue
                    ? await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId.Value && t.Status == "Available")
                    : await _context.Tickets.FirstOrDefaultAsync(t => t.ConcertId == concert.Id && t.Status == "Available");

                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Немає доступних квитків для цього концерту.";
                    return RedirectToAction(nameof(Index));
                }

                // Створюємо запис про покупку
                var purchase = new Purchase
                {
                    SpectatorId = existingSpectator.Id,
                    PurchaseDate = DateTime.Now,
                    Status = "Completed"
                };
                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                // Створюємо елемент покупки
                var purchaseItem = new PurchaseItem
                {
                    PurchaseId = purchase.Id,
                    TicketId = ticket.Id,
                    Quantity = 1,
                    Price = ticket.BasePrice
                };
                _context.PurchaseItems.Add(purchaseItem);

                // Оновлюємо статус квитка та кількість доступних квитків
                ticket.Status = "Sold";
                concert.AvailableTickets--;
                _context.Update(ticket);
                _context.Update(concert);

                // Зберігаємо всі зміни в базі даних
                await _context.SaveChangesAsync();

                // Підтверджуємо транзакцію
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Квиток успішно куплено!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // У разі помилки відкочуємо транзакцію
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Помилка при покупці: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: Concerts/MyTickets
        [Authorize(Roles = "Viewer")]
        public async Task<IActionResult> MyTickets()
        {
            // Отримуємо автентифікованого користувача
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Знаходимо глядача за email користувача
            var spectator = await _context.Spectators
                .FirstOrDefaultAsync(s => s.Email == user.Email);
            if (spectator == null)
            {
                TempData["ErrorMessage"] = "Глядач не знайдений. Спочатку придбайте квиток.";
                return RedirectToAction(nameof(Index));
            }

            // Отримуємо всі покупки глядача з пов'язаними даними
            var purchases = await _context.Purchases
                .Where(p => p.SpectatorId == spectator.Id)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Ticket)
                        .ThenInclude(t => t.Concert)
                            .ThenInclude(c => c.Artist)
                .ToListAsync();

            return View(purchases);
        }
        private bool ConcertExists(int id)
        {
            return _context.Concerts.Any(e => e.Id == id);
        }
    }
}