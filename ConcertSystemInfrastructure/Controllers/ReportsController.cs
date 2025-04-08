using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System.Threading.Tasks;
using System.Linq;
using System;
using OfficeOpenXml;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace ConcertSystemInfrastructure.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ConcertTicketSystemContext _context;

        public ReportsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.Artists = _context.Artists.Select(a => a.FullName).Distinct().ToList();
            ViewBag.Locations = _context.Concerts.Select(c => c.Location).Distinct().ToList();
            ViewBag.Genres = _context.Genres.Select(g => g.Name).Distinct().ToList();
            return View();
        }

        public IActionResult ExportFilter()
        {
            ViewBag.Artists = _context.Artists.Select(a => a.FullName).Distinct().ToList();
            ViewBag.Locations = _context.Concerts.Select(c => c.Location).Distinct().ToList();
            ViewBag.Genres = _context.Genres.Select(g => g.Name).Distinct().ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file, string tableName)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Будь ласка, виберіть файл для імпорту.";
                return RedirectToAction(nameof(Index));
            }

            var errors = new List<string>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        TempData["ErrorMessage"] = "Файл Excel порожній або пошкоджений.";
                        return RedirectToAction(nameof(Index));
                    }

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount <= 1)
                    {
                        TempData["ErrorMessage"] = "Файл Excel не містить даних (окрім заголовків).";
                        return RedirectToAction(nameof(Index));
                    }

                    switch (tableName)
                    {
                        case "Concerts":
                            for (int row = 2; row <= rowCount; row++)
                            {
                                try
                                {
                                    // Очікуємо: Артист, Дата, Місто, Загальна кількість квитків, Доступні квитки, Жанри
                                    string artistName = worksheet.Cells[row, 1].Text?.Trim() ?? "";
                                    if (string.IsNullOrEmpty(artistName))
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Артист' не може бути порожнім.");
                                        continue;
                                    }

                                    var artist = await _context.Artists.FirstOrDefaultAsync(a => a.FullName == artistName);
                                    if (artist == null)
                                    {
                                        errors.Add($"Рядок {row}: Артист '{artistName}' не знайдений у базі даних.");
                                        continue;
                                    }

                                    string dateStr = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                                    if (!DateTime.TryParse(dateStr, out DateTime concertDate))
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Дата' має некоректний формат. Очікується формат 'дд.ММ.рррр'.");
                                        continue;
                                    }

                                    DateTime minAllowedDate = DateTime.Now.AddMonths(1);
                                    if (concertDate < minAllowedDate)
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Дата' - концерт має бути запланований щонайменше за місяць від поточної дати.");
                                        continue;
                                    }

                                    string location = worksheet.Cells[row, 3].Text?.Trim() ?? "";
                                    if (string.IsNullOrEmpty(location))
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Місто' не може бути порожнім.");
                                        continue;
                                    }

                                    string totalTicketsStr = worksheet.Cells[row, 4].Text?.Trim() ?? "";
                                    if (!int.TryParse(totalTicketsStr, out int totalTickets) || totalTickets <= 0)
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Загальна кількість квитків' має бути цілим числом більше 0.");
                                        continue;
                                    }

                                    string availableTicketsStr = worksheet.Cells[row, 5].Text?.Trim() ?? "";
                                    if (!int.TryParse(availableTicketsStr, out int availableTickets) || availableTickets < 0)
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Доступні квитки' має бути цілим числом, не меншим за 0.");
                                        continue;
                                    }

                                    if (availableTickets > totalTickets)
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Доступні квитки' не може бути більшим за 'Загальна кількість квитків'.");
                                        continue;
                                    }

                                    string genresStr = worksheet.Cells[row, 6].Text?.Trim() ?? "";
                                    var genreNames = genresStr?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(g => g.Trim()).ToList() ?? new List<string>();
                                    var genres = new List<Genre>();
                                    foreach (var genreName in genreNames)
                                    {
                                        var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == genreName);
                                        if (genre == null)
                                        {
                                            errors.Add($"Рядок {row}: Жанр '{genreName}' не знайдений у базі даних.");
                                            continue;
                                        }
                                        genres.Add(genre);
                                    }

                                    var concert = new Concert
                                    {
                                        ArtistId = artist.Id,
                                        ConcertDate = concertDate,
                                        Location = location,
                                        TotalTickets = totalTickets,
                                        AvailableTickets = availableTickets,
                                        Genres = genres
                                    };

                                    _context.Concerts.Add(concert);
                                    await _context.SaveChangesAsync();

                                    // Додаємо квитки
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
                                }
                                catch (Exception ex)
                                {
                                    errors.Add($"Рядок {row}: Помилка при імпорті: {ex.Message}");
                                }
                            }
                            break;

                        case "Artist":
                            for (int row = 2; row <= rowCount; row++)
                            {
                                try
                                {
                                    // Очікуємо: Назва, Соціальні мережі
                                    string fullName = worksheet.Cells[row, 1].Text?.Trim() ?? "";
                                    if (string.IsNullOrEmpty(fullName))
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Назва' не може бути порожнім.");
                                        continue;
                                    }

                                    string socialMedia = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                                    if (string.IsNullOrEmpty(socialMedia))
                                    {
                                        errors.Add($"Рядок {row}: Стовпець 'Соціальні мережі' не може бути порожнім.");
                                        continue;
                                    }

                                    if (await _context.Artists.AnyAsync(a => a.FullName == fullName))
                                    {
                                        errors.Add($"Рядок {row}: Артист із назвою '{fullName}' уже існує.");
                                        continue;
                                    }

                                    if (await _context.Artists.AnyAsync(a => a.SocialMedia == socialMedia))
                                    {
                                        errors.Add($"Рядок {row}: Соціальна мережа '{socialMedia}' уже використовується іншим артистом.");
                                        continue;
                                    }

                                    var artist = new Artist
                                    {
                                        FullName = fullName,
                                        SocialMedia = socialMedia
                                    };

                                    _context.Artists.Add(artist);
                                    await _context.SaveChangesAsync();
                                }
                                catch (Exception ex)
                                {
                                    errors.Add($"Рядок {row}: Помилка при імпорті: {ex.Message}");
                                }
                            }
                            break;

                        default:
                            TempData["ErrorMessage"] = "Непідтримувана таблиця для імпорту.";
                            return RedirectToAction(nameof(Index));
                    }
                }
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = "Деякі дані не вдалося імпортувати через помилки.";
                TempData["ImportErrors"] = JsonConvert.SerializeObject(errors);
            }
            else
            {
                TempData["SuccessMessage"] = "Дані успішно імпортовані з Excel!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ExportExcelWithFilter(string tableName, string artistFilter, string locationFilter, string genreFilter, DateTime? dateFilter)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(tableName);

                switch (tableName)
                {
                    case "Concerts":
                        var concertsQuery = _context.Concerts
                            .Include(c => c.Artist)
                            .Include(c => c.Genres)
                            .AsQueryable();

                        if (!string.IsNullOrEmpty(artistFilter))
                            concertsQuery = concertsQuery.Where(c => c.Artist.FullName.Contains(artistFilter));
                        if (!string.IsNullOrEmpty(locationFilter))
                            concertsQuery = concertsQuery.Where(c => c.Location.Contains(locationFilter));
                        if (!string.IsNullOrEmpty(genreFilter))
                            concertsQuery = concertsQuery.Where(c => c.Genres.Any(g => g.Name == genreFilter));
                        if (dateFilter.HasValue)
                            concertsQuery = concertsQuery.Where(c => c.ConcertDate.Date == dateFilter.Value.Date);

                        var concerts = await concertsQuery.ToListAsync();
                        worksheet.Cells[1, 1].Value = "Артист";
                        worksheet.Cells[1, 2].Value = "Дата";
                        worksheet.Cells[1, 3].Value = "Місто";
                        worksheet.Cells[1, 4].Value = "Загальна кількість квитків";
                        worksheet.Cells[1, 5].Value = "Доступні квитки";
                        worksheet.Cells[1, 6].Value = "Жанри";

                        int row = 2;
                        foreach (var concert in concerts)
                        {
                            worksheet.Cells[row, 1].Value = concert.Artist.FullName;
                            worksheet.Cells[row, 2].Value = concert.ConcertDate.ToString("dd.MM.yyyy");
                            worksheet.Cells[row, 3].Value = concert.Location;
                            worksheet.Cells[row, 4].Value = concert.TotalTickets;
                            worksheet.Cells[row, 5].Value = concert.AvailableTickets;
                            worksheet.Cells[row, 6].Value = string.Join(", ", concert.Genres.Select(g => g.Name));
                            row++;
                        }
                        break;

                    case "Artist":
                        var artists = await _context.Artists.ToListAsync();
                        worksheet.Cells[1, 1].Value = "Назва";
                        worksheet.Cells[1, 2].Value = "Соціальні мережі";
                        row = 2;
                        foreach (var artist in artists)
                        {
                            worksheet.Cells[row, 1].Value = artist.FullName;
                            worksheet.Cells[row, 2].Value = artist.SocialMedia;
                            row++;
                        }
                        break;

                    case "Spectators":
                        var spectators = await _context.Spectators.ToListAsync();
                        worksheet.Cells[1, 1].Value = "ПІБ";
                        worksheet.Cells[1, 2].Value = "Телефон";
                        worksheet.Cells[1, 3].Value = "Електронна пошта";
                        row = 2;
                        foreach (var spectator in spectators)
                        {
                            worksheet.Cells[row, 1].Value = spectator.FullName;
                            worksheet.Cells[row, 2].Value = spectator.Phone;
                            worksheet.Cells[row, 3].Value = spectator.Email;
                            row++;
                        }
                        break;
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{tableName}.xlsx");
            }
        }
    }
}