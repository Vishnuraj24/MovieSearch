namespace MovieSearch.Movies.Infrastructure.Elasticsearch
{
    public sealed class ElasticsearchOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:9200";
        public string IndexName { get; set; } = "movies";
    }
}
