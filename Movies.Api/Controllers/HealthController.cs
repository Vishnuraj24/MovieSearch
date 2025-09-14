using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieSearch.Movies.Infrastructure.Elasticsearch;

namespace MovieSearch.Movies.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class HealthController : ControllerBase
    {
        private readonly ElasticsearchHttp _es;
        private readonly ElasticsearchOptions _options;

        public HealthController(ElasticsearchHttp es, ElasticsearchOptions options)
        {
            _es = es; _options = options;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var (ok, body, status) = await _es.GetAsync("/_cluster/health?pretty");
            if (!ok) return StatusCode(status, body);
            return Content(body, "application/json");
        }
    }
}
