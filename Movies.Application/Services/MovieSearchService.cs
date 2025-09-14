using MovieSearch.Movies.Application.Interfaces;
using MovieSearch.Movies.Domain;
using MovieSearch.Movies.Infrastructure.Elasticsearch;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace MovieSearch.Movies.Application.Services
{
    public sealed class MovieSearchService : IMovieSearchService
    {
        private readonly ElasticsearchHttp _es;
        private readonly ElasticsearchOptions _options;

        public MovieSearchService(ElasticsearchHttp es, ElasticsearchOptions options)
        {
            _es = es;
            _options = options;
        }

        public async Task IndexAsync(Movie m)
        {
            var body = new
            {
                id = m.Id,
                title = m.Title,
                year = m.Year,
                genre = m.Genre,
                description = m.Description,
                cast = m.Cast,
                title_suggest = new { input = new[] { m.Title } }
            };
            var (ok, resp, status) = await _es.PutJsonAsync($"/{_options.IndexName}/_doc/{m.Id}", body);
            if (!ok) throw new Exception($"Index failed: {status} {resp}");
        }

        public async Task<(bool ok, string body, int status)> SeedAsync()
        {
            var seed = new[]
            {
                new Movie{ Id="1", Title="Inception", Year=2010, Genre="sci-fi", Description="A thief who steals corporate secrets through dream-sharing.", Cast=new[]{"Leonardo DiCaprio","Joseph Gordon-Levitt"}},
                new Movie{ Id="2", Title="Interstellar", Year=2014, Genre="sci-fi", Description="Explorers travel through a wormhole in space.", Cast=new[]{"Matthew McConaughey","Anne Hathaway"}},
                new Movie{ Id="3", Title="The Dark Knight", Year=2008, Genre="action", Description="Batman faces the Joker.", Cast=new[]{"Christian Bale","Heath Ledger"}},
                new Movie{ Id="4", Title="Whiplash", Year=2014, Genre="drama", Description="A young drummer and an abusive instructor.", Cast=new[]{"Miles Teller","J.K. Simmons"}},
                new Movie{ Id="5", Title="La La Land", Year=2016, Genre="romance", Description="A jazz musician and an actress fall in love.", Cast=new[]{"Ryan Gosling","Emma Stone"}},
                new Movie{ Id="6", Title="The Matrix", Year=1999, Genre="sci-fi", Description="A hacker discovers the truth about his reality.", Cast=new[]{"Keanu Reeves","Carrie-Anne Moss"}},
                new Movie{ Id="7", Title="Mad Max: Fury Road", Year=2015, Genre="action", Description="A post-apocalyptic chase.", Cast=new[]{"Tom Hardy","Charlize Theron"}},
                new Movie{ Id="8", Title="Parasite", Year=2019, Genre="thriller", Description="A poor family schemes to become employed by a wealthy family.", Cast=new[]{"Song Kang-ho","Cho Yeo-jeong"}}
            };

            var sb = new StringBuilder();
            foreach (var m in seed)
            {
                // ACTION line
                sb.AppendLine(JsonSerializer.Serialize(new { index = new { _index = _options.IndexName, _id = m.Id } }));
                // SOURCE line
                sb.AppendLine(JsonSerializer.Serialize(new
                {
                    id = m.Id,
                    title = m.Title,
                    year = m.Year,
                    genre = m.Genre,
                    description = m.Description,
                    cast = m.Cast,
                    title_suggest = new { input = new[] { m.Title } }
                }));
            }

            // Ensure the bulk body ends with a newline
            if (sb.Length == 0 || sb[sb.Length - 1] != '\n')
                sb.Append('\n');

            // IMPORTANT: use PostNdjsonAsync (NOT PostJsonAsync)
            var (ok, body, status) = await _es.PostNdjsonAsync("/_bulk?refresh=true", sb.ToString());

            return (ok, body, status);
        }
        public async Task<string> SearchAsync(string? q, string? genre, int? yearFrom, int? yearTo, int page, int pageSize, string? sort)
        {
            var from = Math.Max(0, (page - 1) * pageSize);
            var must = new List<object>();
            if (!string.IsNullOrWhiteSpace(q))
            {
                must.Add(new
                {
                    multi_match = new
                    {
                        query = q,
                        fields = new[] { "title^3", "description", "cast^2" },
                        fuzziness = "AUTO"
                    }
                });
            }
            else
            {
                must.Add(new { match_all = new { } });
            }

            var filters = new List<object>();
            if (!string.IsNullOrWhiteSpace(genre))
                filters.Add(new { term = new Dictionary<string, object> { ["genre"] = genre! } });
            if (yearFrom is not null || yearTo is not null)
            {
                var range = new Dictionary<string, object>();
                if (yearFrom is not null) range["gte"] = yearFrom;
                if (yearTo is not null) range["lte"] = yearTo;
                filters.Add(new { range = new Dictionary<string, object> { ["year"] = range } });
            }

            var sortSpec = new List<object>();
            if (!string.IsNullOrWhiteSpace(sort) && sort.Contains(':'))
            {
                var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
                sortSpec.Add(new Dictionary<string, object> { [parts[0]] = new { order = parts[1] } });
            }
            else
            {
                sortSpec.Add(new Dictionary<string, object> { ["_score"] = new { order = "desc" } });
            }

            var body = new
            {
                from,
                size = pageSize,
                sort = sortSpec,
                query = new
                {
                    @bool = new
                    {
                        must,
                        filter = filters
                    }
                },
                aggs = new
                {
                    genres = new { terms = new { field = "genre", size = 20 } },
                    years = new { histogram = new { field = "year", interval = 5 } }
                },
                highlight = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["title"] = new { },
                        ["description"] = new { }
                    }
                }
            };

            var (ok, text, status) = await _es.PostJsonAsync($"/{_options.IndexName}/_search", body);
            if (!ok) throw new Exception($"Search failed: {status} {text}");
            return text;
        }

        public async Task<string> SuggestAsync(string q, int size)
        {
            var body = new
            {
                suggest = new
                {
                    title_suggest = new
                    {
                        prefix = q,
                        completion = new { field = "title_suggest", size, skip_duplicates = true }
                    }
                }
            };
            var (ok, text, status) = await _es.PostJsonAsync($"/{_options.IndexName}/_search", body);
            if (!ok) throw new Exception($"Suggest failed: {status} {text}");
            return text;
        }

        public async Task<string> GetByIdAsync(string id)
        {
            var (ok, text, status) = await _es.GetAsync($"/{_options.IndexName}/_doc/{id}");
            if (!ok) throw new Exception($"Get failed: {status} {text}");
            return text;
        }

        public async Task<string> DeleteAsync(string id)
        {
            var (ok, text, status) = await _es.DeleteAsync($"/{_options.IndexName}/_doc/{id}");
            if (!ok) throw new Exception($"Delete failed: {status} {text}");
            return text;
        }

        public async Task<(bool ok, string body, int status)> UpdatePartialAsync(string id, JsonElement partialDoc)
        {
            // Sends: POST /{index}/_update/{id}  { "doc": { ...fields to change... } }
            var body = new { doc = partialDoc };
            return await _es.PostJsonAsync($"/{_options.IndexName}/_update/{id}", body);
        }
    }

    // Small helper type to send NDJSON via PostJsonAsync without re-serializing
    file sealed class RawNdJson
    {
        public string Value { get; }
        public RawNdJson(string value) => Value = value;
    }

    file static class ElasticsearchHttpNdJsonExtension
    {
        public static async Task<(bool ok, string body, int status)> PostJsonAsync(this ElasticsearchHttp es, string path, RawNdJson ndjson)
        {
            var resp = await es.PostRawAsync(path, ndjson.Value, "application/x-ndjson");
            var text = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, text, (int)resp.StatusCode);
        }

        public static async Task<HttpResponseMessage> PostRawAsync(this ElasticsearchHttp es, string path, string body, string contentType)
        {
            var clientField = typeof(ElasticsearchHttp).GetField("_http", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var http = (HttpClient)clientField!.GetValue(es)!;
            return await http.PostAsync(path, new StringContent(body, Encoding.UTF8, contentType));
        }
    }
}
