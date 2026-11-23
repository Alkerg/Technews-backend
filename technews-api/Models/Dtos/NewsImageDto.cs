namespace technews_api.Models.Dtos
{
    public class NewsImageDto
    {
        public required string Url { get; init; }
        public string? Alt { get; init; }
        public string? Credit { get; init; }
    }
}
