namespace technews_api.Models.Dtos
{
    public class NewsResponseDto
    {
        public required string Url { get; init; }
        public string? Title { get; init; }
        public string? Subtitle { get; init; }
        public List<string> Authors { get; init; } = [];
        public DateTime? PublishedAt { get; init; }
        public string? Category { get; init; }
        public List<string> Tags { get; init; } = [];
        public NewsImageDto? MainImage { get; init; }
        public string? Summary { get; init; }
        public string? Content { get; init; }
        public ContentFormat ContentFormat { get; init; }
    }
}
