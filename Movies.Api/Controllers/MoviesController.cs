using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieSearch.Movies.Application.Interfaces;
using MovieSearch.Movies.Domain;
using System.Text.Json;

namespace MovieSearch.Movies.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class MoviesController : ControllerBase
    {
        private readonly IMovieSearchService _svc;

        public MoviesController(IMovieSearchService svc)
        {
            _svc = svc;
        }

        // POST api/movies
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Movie movie)
        {
            await _svc.IndexAsync(movie);
            return CreatedAtAction(nameof(GetById), new { id = movie.Id }, new { message = "Indexed", movie.Id });
        }

        // POST api/movies/seed
        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            var (ok, body, _) = await _svc.SeedAsync();
            return ok ? Ok(new { message = "Seeded", raw = System.Text.Json.JsonDocument.Parse(body).RootElement })
                      : StatusCode(500, body);
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search(
                 [FromBody] SearchRequest request)
        {
            var json = await _svc.SearchAsync(
                request.Q,
                request.Genre,
                request.YearFrom,
                request.YearTo,
                request.Page ?? 1,
                request.PageSize ?? 10,
                request.Sort);
            return Content(json, "application/json");
        }

        // GET api/movies/suggest?q=la
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string q, [FromQuery] int size = 5)
        {
            var json = await _svc.SuggestAsync(q, size);
            return Content(json, "application/json");
        }

        // GET api/movies/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var json = await _svc.GetByIdAsync(id);
            return Content(json, "application/json");
        }

        // DELETE api/movies/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            var json = await _svc.DeleteAsync(id);
            return Content(json, "application/json");
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePartial([FromRoute] string id, [FromBody] JsonElement partial)
        {
            var (ok, body, status) = await _svc.UpdatePartialAsync(id, partial);
            return ok ? StatusCode(200, body) : StatusCode(status, body);
        }

    }
}
