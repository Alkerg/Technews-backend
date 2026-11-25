using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
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
        private const int DefaultPageSize = 9;
        private const int MaxPageSize = 20;

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
                var start = from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                query = query.Where(n => string.Compare(n.ExtractedAt, start) >= 0);
            }
            if (to is not null)
            {
                var endExclusive = to.Value.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                query = query.Where(n => string.Compare(n.ExtractedAt, endExclusive) < 0);
            }

            var news = await query
                .OrderByDescending(n => n.ExtractedAt)
                .ToListAsync(ct);

            var newsDto = _mapper.Map<List<NewsResponseDto>>(news);
            return Ok(newsDto);

        }

        [HttpGet("byPublishedDate")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<List<NewsResponseDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewsByPublishedDate([FromQuery] DateOnly? date, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
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

            var query = _db.News.AsNoTracking().Where(n => n.PublishedAt != null);

            if (from is not null)
            {
                var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(n => n.PublishedAt >= start);
            }
            if (to is not null)
            {
                var endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(n => n.PublishedAt < endExclusive);
            }

            var news = await query
                .OrderByDescending(n => n.PublishedAt)
                .ToListAsync(ct);

            var newsDto = _mapper.Map<List<NewsResponseDto>>(news);

            return Ok(newsDto);
        }

        [HttpGet("byCategory")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<List<NewsResponseDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewsByCategory([FromQuery] string? category, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("No se ha indicado ninguna categoría.");

            var normalizedCategory = category.Trim().ToLower();
            var query = _db.News.AsNoTracking().Where(n => n.Category!.ToLower() == normalizedCategory);

            var news = await query
                .OrderByDescending(n => n.PublishedAt)
                .ToListAsync(ct);

            var newsDto = _mapper.Map<List<NewsResponseDto>>(news);

            return Ok(newsDto);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<PagedResponseDto<MinimalNewsResponseDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewsByMultipleFilters(
            [FromQuery] string? category,
            [FromQuery] DateOnly? date,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            if (page < 1)
                return BadRequest("'page' debe ser 1 o mayor.");

            if (pageSize < 1 || pageSize > MaxPageSize)
                return BadRequest($"'pageSize' debe estar entre 1 y {MaxPageSize}.");

            if (date is not null)
            {
                if (from is not null || to is not null)
                    return BadRequest("Usa 'date' o 'from'/'to', no ambos.");
                from = to = date;
            }

            if (from > to)
                return BadRequest("'from' no puede ser posterior a 'to'.");

            var query = _db.News.AsNoTracking();

            if (from is not null)
            {
                var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(n => n.PublishedAt >= start);
            }
            if (to is not null)
            {
                var endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(n => n.PublishedAt < endExclusive);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var normalizedCategory = category.Trim().ToLower();
                query = query.Where(n => n.Category!.ToLower() == normalizedCategory);
            }

            var totalItems = await query.LongCountAsync(ct);
            
            var rows = await query
                .OrderByDescending(n => n.PublishedAt)
                .ThenBy(n => n.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.Slug,
                    n.Url,
                    n.Title,
                    n.Subtitle,
                    n.Authors,
                    n.PublishedAt,
                    n.Category,
                    n.Tags,
                    n.Summary,
                    n.MainImage
                })
                .ToListAsync(ct);

            var items = rows.Select(n => new MinimalNewsResponseDto
            {
                Slug = n.Slug,
                Url = n.Url,
                Title = n.Title,
                Subtitle = n.Subtitle,
                Authors = n.Authors,
                PublishedAt = n.PublishedAt,
                Category = n.Category,
                Tags = n.Tags,
                Summary = n.Summary,
                MainImage = n.MainImage is null ? null : new NewsImageDto
                {
                    Url = n.MainImage.Url,
                    Alt = n.MainImage.Alt,
                    Credit = n.MainImage.Credit
                }
            }).ToList();

            return Ok(new PagedResponseDto<MinimalNewsResponseDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }

        [HttpGet("{slug}")]
        [ProducesResponseType<NewsResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNewsBySlug(string slug, CancellationToken ct)
        {
            var news = await _db.News
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == slug, ct);

            return news is null ? NotFound() : Ok(_mapper.Map<NewsResponseDto>(news));
        }

    }
}
