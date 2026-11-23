using System.ComponentModel.DataAnnotations;

namespace technews_api.Models
{
    public class News
    {
        [Key]
        public string Id { get; set; }
        public string? Title { get; set; }
        public string Url { get; set; }
        public string? Subtitle { get; set; }
        public List<string> Authors { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? Category { get; set; }
        public List<string> Tags { get; set; } = [];
        public NewsImage? MainImage { get; set; }
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public ContentFormat ContentFormat { get; set; } = ContentFormat.Markdown;
        //public ExtractionStatus Status { get; set; } 
        //public string? Error { get; set; }

        public DateTime ExtractedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }



    public enum ContentFormat { Markdown, Html, Text }
    public enum ExtractionStatus { Ok, Error }
}
