namespace technews_api.Models.Dtos
{
    public class NewsIngestDto
    {
        public required string Id { get; init; }
        public required string Url { get; init; }
        public string? Title { get; init; }
        public string? Subtitle { get; init; }
        public List<string>? Authors { get; init; }
        public DateTimeOffset? PublishedAt { get; init; }
        public string? Category { get; init; }
        public List<string>? Tags { get; init; }
        public NewsImageDto? MainImage { get; init; }
        public string? Summary { get; init; }
        public string? Content { get; init; }
        public ContentFormat? ContentFormat { get; init; }
        //public string? Language { get; init; }
        //public required ExtractionStatus Status { get; init; }
        //public string? Error { get; init; }
        public required DateTimeOffset ExtractedAt { get; init; }
    }
}
