using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using technews_api.Data;
using technews_api.Models;
using technews_api.Models.Dtos;

namespace technews_api.Controllers
{
    [ApiController]
    [Route("api/news")]
    public class NewsController : ControllerBase
    {
        NewsDbContext _db;
        IMapper _mapper;
        public NewsController(NewsDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IngestNews([FromBody] List<NewsIngestDto> news, CancellationToken ct)
        {
            if (news is null || news.Count == 0)
                return BadRequest("La lista de noticias está vacía.");

            // Quitar duplicados dentro del mismo lote
            var batch = news.GroupBy(n => n.Id).Select(g => g.First()).ToList();
            var ids = batch.Select(n => n.Id).ToList();

            // Consultar cuáles ya existen en la BD (se traduce a un $in)
            var existingIds = (await _db.News
                    .Where(n => ids.Contains(n.Id))
                    .Select(n => n.Id)
                    .ToListAsync(ct))
                .ToHashSet();

            // Insertar solo las nuevas
            var now = DateTime.UtcNow;
            var toInsert = batch
                .Where(n => !existingIds.Contains(n.Id))
                .Select(n =>
                {
                    var entity = _mapper.Map<News>(n);
                    return entity;
                })
                .ToList();

            if (toInsert.Count > 0)
            {
                _db.News.AddRange(toInsert);
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new
            {
                received = news.Count,
                inserted = toInsert.Count,
                skipped = news.Count - toInsert.Count
            });
        }


        [HttpGet]
        [ProducesResponseType<List<NewsResponseDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNews(CancellationToken ct)
        {
            var news = await _db.News
                .AsNoTracking()                         
                .OrderByDescending(n => n.PublishedAt)  
                .ToListAsync(ct);

            var newsDto = _mapper.Map<List<NewsResponseDto>>(news);
            
            return Ok(newsDto);
        }

        [HttpGet("byExtractionDate")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<List<NewsResponseDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewsByExtractionDate([FromQuery] DateOnly? date, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            if (date is not null)
            {
                if (from is not null || to is not null)
                    return BadRequest("Usa 'date' o 'from'/'to', no ambos.");
                from = to = date;
            }

            if (from is null && to is null)
                return BadRequest("Indica 'date' o al menos uno de 'from'/'to' (formato yyyy-MM-dd).");

            if (from > to)
                return BadRequest("'from' no puede ser posterior a 'to'.");

            var query = _db.News.AsNoTracking();

            if (from is not null)
            {
                var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(n => n.ExtractedAt >= start);
            }
            if (to is not null)
            {
                var endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(n => n.ExtractedAt < endExclusive);
            }

            var news = await query
                .OrderByDescending(n => n.ExtractedAt)
                .ToListAsync(ct);

            var newsDto = _mapper.Map<List<NewsResponseDto>>(news);
            return Ok(newsDto);

        }

    }
}
