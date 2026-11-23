using Mapster;

using technews_api.Models;
using technews_api.Models.Dtos;

namespace technews_api.Mapping
{
    public class NewsMappingConfig: IRegister
    {

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<NewsIngestDto, News>()
                .Map(dest => dest.Id, src => src.Id)         
                .Ignore(dest => dest.CreatedAt)   
                .Ignore(dest => dest.UpdatedAt)
                .Map(dest => dest.PublishedAt,
                     src => src.PublishedAt.HasValue ? src.PublishedAt.Value.UtcDateTime : (DateTime?)null)
                .Map(dest => dest.ExtractedAt, src => src.ExtractedAt.UtcDateTime)
                .Map(dest => dest.Authors, src => src.Authors ?? new List<string>())
                .Map(dest => dest.Tags, src => src.Tags ?? new List<string>())
                .Map(dest => dest.ContentFormat, src => src.ContentFormat ?? ContentFormat.Markdown);
        }

    }
}
