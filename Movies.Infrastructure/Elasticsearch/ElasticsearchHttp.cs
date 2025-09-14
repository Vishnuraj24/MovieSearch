using System.Text;
using System.Text.Json;

namespace MovieSearch.Movies.Infrastructure.Elasticsearch
{
    public sealed class ElasticsearchHttp
    {
        private readonly HttpClient _http;

        public ElasticsearchHttp(HttpClient http)
        {
            _http = http;
        }

        public async Task<(bool ok, string body, int status)> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            var text = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, text, (int)resp.StatusCode);
        }

        public async Task<(bool ok, string body, int status)> DeleteAsync(string path)
        {
            var resp = await _http.DeleteAsync(path);
            var text = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, text, (int)resp.StatusCode);
        }

        public async Task<(bool ok, string body, int status)> PostJsonAsync(string path, object body)
        {
            var json = JsonSerializer.Serialize(body);
            var resp = await _http.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, text, (int)resp.StatusCode);
        }

        public async Task<(bool ok, string body, int status)> PutJsonAsync(string path, object body)
        {
            var json = JsonSerializer.Serialize(body);
            var resp = await _http.PutAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, text, (int)resp.StatusCode);
        }

        public async Task<(bool ok, string body, int status)> PostNdjsonAsync(string path, string ndjson)
        {
            var content = new StringContent(ndjson, Encoding.UTF8, "application/x-ndjson");
            var resp = await _http.PostAsync(path, content);
            var text = await resp.Content.ReadAsStringAsync();
            return (resp.IsSuccessStatusCode, text, (int)resp.StatusCode);
        }
    }
}
