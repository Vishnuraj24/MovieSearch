namespace MovieSearch.Movies.Infrastructure.Elasticsearch
{
    public interface IElasticsearchIndexInitializer
    {
        Task EnsureIndexAsync();
    }

    public sealed class ElasticsearchIndexInitializer : IElasticsearchIndexInitializer
    {
        private readonly ElasticsearchHttp _es;
        private readonly ElasticsearchOptions _options;

        public ElasticsearchIndexInitializer(ElasticsearchHttp es, ElasticsearchOptions options)
        {
            _es = es;
            _options = options;
        }

        public async Task EnsureIndexAsync()
        {
            // HEAD /{index}
            using var head = new HttpRequestMessage(HttpMethod.Head, $"/{_options.IndexName}");
            var resp = await _es.HttpSendAsync(head);
            if (resp.IsSuccessStatusCode) return; // index exists

            var createIndexBody = new
            {
                settings = new
                {
                    analysis = new
                    {
                        analyzer = new
                        {
                            autocomplete = new
                            {
                                tokenizer = "standard",
                                filter = new[] { "lowercase", "edge_ngram_filter" }
                            }
                        },
                        filter = new
                        {
                            edge_ngram_filter = new
                            {
                                type = "edge_ngram",
                                min_gram = 2,
                                max_gram = 20
                            }
                        }
                    }
                },
                mappings = new
                {
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new
                        {
                            type = "text",
                            analyzer = "autocomplete",
                            search_analyzer = "standard",
                            fields = new { keyword = new { type = "keyword" } }
                        },
                        ["title_suggest"] = new { type = "completion" },
                        ["description"] = new { type = "text" },
                        ["genre"] = new { type = "keyword" },
                        ["year"] = new { type = "integer" },
                        ["cast"] = new { type = "keyword" }
                    }
                }
            };

            var (ok, body, status) = await _es.PutJsonAsync($"/{_options.IndexName}", createIndexBody);
            if (!ok)
            {
                throw new Exception($"Failed to create index: status={status} body={body}");
            }
        }
    }

    internal static class ElasticsearchHttpExtensions
    {
        public static async Task<HttpResponseMessage> HttpSendAsync(this ElasticsearchHttp es, HttpRequestMessage msg)
        {
            // Little helper so we can do HEAD requests in initializer
            var clientField = typeof(ElasticsearchHttp).GetField("_http", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var http = (HttpClient)clientField!.GetValue(es)!;
            return await http.SendAsync(msg);
        }
    }
}
