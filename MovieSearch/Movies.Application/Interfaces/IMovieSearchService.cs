using MovieSearch.Movies.Domain;
using System.Text.Json;

namespace MovieSearch.Movies.Application.Interfaces
{
    public interface IMovieSearchService
    {
        Task IndexAsync(Movie movie);
        Task<(bool ok, string body, int status)> SeedAsync();
        Task<string> SearchAsync(string? q, string? genre, int? yearFrom, int? yearTo, int page, int pageSize, string? sort);
        Task<string> SuggestAsync(string q, int size);
        Task<string> GetByIdAsync(string id);
        Task<string> DeleteAsync(string id);
        Task<(bool ok, string body, int status)> UpdatePartialAsync(string id, JsonElement partialDoc);

    }
}
