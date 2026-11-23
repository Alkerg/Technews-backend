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



    }
}
