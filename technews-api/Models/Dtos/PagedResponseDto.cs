namespace technews_api.Models.Dtos
{
    public class PagedResponseDto<T>
    {
        public required List<T> Items { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long TotalItems { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}
