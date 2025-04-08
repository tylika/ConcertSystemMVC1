using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;
using System.Threading.Tasks;
using System.Linq;
using System;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;
using DocumentFormat.OpenXml;

namespace ConcertSystemInfrastructure.Controllers
{
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
                        //worksheet.Cells[1, 1].Value = "ID";
                        worksheet.Cells[1, 2].Value = "Артист";
                        worksheet.Cells[1, 3].Value = "Дата";
                        worksheet.Cells[1, 4].Value = "Місто";
                        worksheet.Cells[1, 5].Value = "Загальна кількість квитків";
                        worksheet.Cells[1, 6].Value = "Доступні квитки";
                        worksheet.Cells[1, 7].Value = "Жанри";

                        int row = 2;
                        foreach (var concert in concerts)
                        {
                           // worksheet.Cells[row, 1].Value = concert.Id;
                            worksheet.Cells[row, 2].Value = concert.Artist.FullName;
                            worksheet.Cells[row, 3].Value = concert.ConcertDate.ToString("dd.MM.yyyy");
                            worksheet.Cells[row, 4].Value = concert.Location;
                            worksheet.Cells[row, 5].Value = concert.TotalTickets;
                            worksheet.Cells[row, 6].Value = concert.AvailableTickets;
                            worksheet.Cells[row, 7].Value = string.Join(", ", concert.Genres.Select(g => g.Name));
                            row++;
                        }
                        break;

                    case "Artist":
                        var artists = await _context.Artists.ToListAsync();
                        //worksheet.Cells[1, 1].Value = "ID";
                        worksheet.Cells[1, 2].Value = "Назва";
                        worksheet.Cells[1, 3].Value = "Соціальні мережі";
                        row = 2;
                        foreach (var artist in artists)
                        {
                           // worksheet.Cells[row, 1].Value = artist.Id;
                            worksheet.Cells[row, 2].Value = artist.FullName;
                            worksheet.Cells[row, 3].Value = artist.SocialMedia;
                            row++;
                        }
                        break;

                    case "Spectators":
                        var spectators = await _context.Spectators.ToListAsync();
                       // worksheet.Cells[1, 1].Value = "ID";
                        worksheet.Cells[1, 2].Value = "ПІБ";
                        worksheet.Cells[1, 3].Value = "Телефон";
                        worksheet.Cells[1, 4].Value = "Електронна пошта";
                        row = 2;
                        foreach (var spectator in spectators)
                        {
                           // worksheet.Cells[row, 1].Value = spectator.Id;
                            worksheet.Cells[row, 2].Value = spectator.FullName;
                            worksheet.Cells[row, 3].Value = spectator.Phone;
                            worksheet.Cells[row, 4].Value = spectator.Email;
                            row++;
                        }
                        break;

                        // Додай інші таблиці за потреби (Purchases, Tickets)
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{tableName}.xlsx");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportDocxWithFilter(string tableName, string artistFilter, string locationFilter, string genreFilter, DateTime? dateFilter)
        {
            using (var stream = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
                {
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

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
                            body.AppendChild(new Paragraph(new Run(new Text("Концерти"))));
                            foreach (var concert in concerts)
                            {
                                //body.AppendChild(new Paragraph(new Run(new Text($"ID: {concert.Id}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Артист: {concert.Artist.FullName}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Дата: {concert.ConcertDate:dd.MM.yyyy}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Місто: {concert.Location}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Квитки: {concert.TotalTickets}/{concert.AvailableTickets}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Жанри: {string.Join(", ", concert.Genres.Select(g => g.Name))}"))));
                                body.AppendChild(new Paragraph(new Run(new Text(""))));
                            }
                            break;

                        case "Artist":
                            var artists = await _context.Artists.ToListAsync();
                            body.AppendChild(new Paragraph(new Run(new Text("Виконавці"))));
                            foreach (var artist in artists)
                            {
                               // body.AppendChild(new Paragraph(new Run(new Text($"ID: {artist.Id}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Назва: {artist.FullName}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Соціальні мережі: {artist.SocialMedia}"))));
                                body.AppendChild(new Paragraph(new Run(new Text(""))));
                            }
                            break;

                        case "Spectators":
                            var spectators = await _context.Spectators.ToListAsync();
                            body.AppendChild(new Paragraph(new Run(new Text("Глядачі"))));
                            foreach (var spectator in spectators)
                            {
                                //body.AppendChild(new Paragraph(new Run(new Text($"ID: {spectator.Id}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"ПІБ: {spectator.FullName}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Телефон: {spectator.Phone}"))));
                                body.AppendChild(new Paragraph(new Run(new Text($"Електронна пошта: {spectator.Email}"))));
                                body.AppendChild(new Paragraph(new Run(new Text(""))));
                            }
                            break;

                            // Додай інші таблиці за потреби
                    }
                }

                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{tableName}.docx");
            }
        }
    }
}