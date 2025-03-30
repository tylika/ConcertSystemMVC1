using ConcertSystemDomain.Model;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ConcertSystemDomain.Model; // Змінити на ваш namespace
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConcertSystemInfrastructure.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ConcertTicketSystemContext _context; 

        public ReportsController(ConcertTicketSystemContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; 
        }

        
        public IActionResult Index()
        {
            return View();
        }

        // POST: Імпорт з Excel
        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file, string tableName)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Будь ласка, виберіть файл для імпорту.";
                return RedirectToAction(nameof(Index));
            }

            if (tableName != "Concerts") 
            {
                TempData["ErrorMessage"] = "Непідтримувана таблиця.";
                return RedirectToAction(nameof(Index));
            }

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Пропускаємо заголовок
                {
                    var concert = new Concert 
                    {
                        ArtistId = int.TryParse(worksheet.Cells[row, 1].Value?.ToString(), out int artistId) ? artistId : 0,
                        ConcertDate = DateTime.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out DateTime date) ? date : DateTime.Now,
                        Location = worksheet.Cells[row, 3].Value?.ToString(),
                        TotalTickets = int.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out int total) ? total : 0,
                        AvailableTickets = int.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out int available) ? available : 0
                    };

                    if (concert.ArtistId > 0 && !string.IsNullOrEmpty(concert.Location))
                    {
                        _context.Concerts.Add(concert); 
                    }
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Концерти успішно імпортовано!";
            return RedirectToAction("Index", "Concerts"); 
        }

        // GET: Експорт у Excel
        public async Task<IActionResult> ExportExcel(string tableName)
        {
            if (tableName != "Concerts")
            {
                return BadRequest("Непідтримувана таблиця.");
            }

            var concerts = await _context.Concerts.ToListAsync(); 
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Concerts");
                worksheet.Cells[1, 1].Value = "ArtistId";
                worksheet.Cells[1, 2].Value = "Concert Date";
                worksheet.Cells[1, 3].Value = "Location";
                worksheet.Cells[1, 4].Value = "Total Tickets";
                worksheet.Cells[1, 5].Value = "Available Tickets";

                for (int i = 0; i < concerts.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = concerts[i].ArtistId;
                    worksheet.Cells[i + 2, 2].Value = concerts[i].ConcertDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cells[i + 2, 3].Value = concerts[i].Location;
                    worksheet.Cells[i + 2, 4].Value = concerts[i].TotalTickets;
                    worksheet.Cells[i + 2, 5].Value = concerts[i].AvailableTickets;
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                string fileName = $"ConcertsReport_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // POST: Імпорт з DOCX
        [HttpPost]
        public async Task<IActionResult> ImportDocx(IFormFile file, string tableName)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Будь ласка, виберіть файл для імпорту.";
                return RedirectToAction(nameof(Index));
            }

            if (tableName != "Concerts")
            {
                TempData["ErrorMessage"] = "Непідтримувана таблиця.";
                return RedirectToAction(nameof(Index));
            }

            using (var stream = file.OpenReadStream())
            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, false))
            {
                string text = doc.MainDocumentPart.Document.Body.InnerText;
                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines.Skip(1)) 
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        var concert = new Concert 
                        {
                            ArtistId = int.TryParse(parts[0].Trim(), out int artistId) ? artistId : 0,
                            ConcertDate = DateTime.TryParse(parts[1].Trim(), out DateTime date) ? date : DateTime.Now,
                            Location = parts[2].Trim(),
                            TotalTickets = int.TryParse(parts[3].Trim(), out int total) ? total : 0,
                            AvailableTickets = int.TryParse(parts[4].Trim(), out int available) ? available : 0
                        };

                        if (concert.ArtistId > 0 && !string.IsNullOrEmpty(concert.Location))
                        {
                            _context.Concerts.Add(concert); 
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Концерти успішно імпортовано з DOCX!";
            return RedirectToAction("Index", "Concerts"); 
        }

        // GET: Експорт у DOCX
        public async Task<IActionResult> ExportDocx(string tableName)
        {
            if (tableName != "Concerts")
            {
                return BadRequest("Непідтримувана таблиця.");
            }

            var concerts = await _context.Concerts.ToListAsync(); 
            var stream = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                Paragraph title = body.AppendChild(new Paragraph());
                Run titleRun = title.AppendChild(new Run());
                titleRun.AppendChild(new Text("Звіт про концерти"));

                foreach (var concert in concerts)
                {
                    Paragraph para = body.AppendChild(new Paragraph());
                    Run run = para.AppendChild(new Run());
                    run.AppendChild(new Text($"{concert.ArtistId} | {concert.ConcertDate:yyyy-MM-dd HH:mm} | {concert.Location} | {concert.TotalTickets} | {concert.AvailableTickets}"));
                }
            }

            stream.Position = 0;
            string fileName = $"ConcertsReport_{DateTime.Now:yyyyMMdd}.docx";
            return File(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
    }
}