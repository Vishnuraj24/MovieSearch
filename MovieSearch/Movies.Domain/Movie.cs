namespace MovieSearch.Movies.Domain
{
    public sealed class Movie
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public int Year { get; set; }
        public string Genre { get; set; } = default!;
        public string? Description { get; set; }
        public string[]? Cast { get; set; }
    }

    // Define a request model to handle the POST body
    public class SearchRequest
    {
        public string? Q { get; set; }
        public string? Genre { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? Sort { get; set; }
    }
}
