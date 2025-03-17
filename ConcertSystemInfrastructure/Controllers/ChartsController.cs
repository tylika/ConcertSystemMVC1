using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model;
using ConcertSystemInfrastructure;

namespace ConcertSystemInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly ConcertTicketSystemContext _context;

        public ChartsController(ConcertTicketSystemContext context)
        {
            _context = context;
        }

        // Діаграма 1: Кількість концертів за роками
        private record ConcertsByYearResponseItem(string Year, int Count);

        [HttpGet("concertsByYear")]
        public async Task<JsonResult> GetConcertsByYearAsync(CancellationToken cancellationToken)
        {
            var responseItems = await _context.Concerts
                .GroupBy(concert => concert.ConcertDate.Year)
                .Select(group => new ConcertsByYearResponseItem(group.Key.ToString(), group.Count()))
                .ToListAsync(cancellationToken);
            return new JsonResult(responseItems);
        }

        // Діаграма 2: Кількість проданих квитків за жанрами
        private record TicketsByGenreResponseItem(string Genre, int Count);

        [HttpGet("ticketsByGenre")]
        public async Task<JsonResult> GetTicketsByGenreAsync(CancellationToken cancellationToken)
        {
            var responseItems = await _context.Tickets
                .Where(t => t.Status == "Sold")
                .Include(t => t.Concert)
                .ThenInclude(c => c.Genres)
                .SelectMany(t => t.Concert.Genres)
                .GroupBy(g => g.Name)
                .Select(group => new TicketsByGenreResponseItem(group.Key, group.Count()))
                .ToListAsync(cancellationToken);
            return new JsonResult(responseItems);
        }
    }
}