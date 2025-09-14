using MovieSearch.Movies.Application.Interfaces;
using MovieSearch.Movies.Application.Services;
using MovieSearch.Movies.Infrastructure;
using MovieSearch.Movies.Infrastructure.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure & config
builder.Services.AddElasticsearch(builder.Configuration);

// Application services
builder.Services.AddSingleton<IMovieSearchService, MovieSearchService>();

var app = builder.Build();

// On startup, ensure the index exists
using (var scope = app.Services.CreateScope())
{
    var indexer = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexInitializer>();
    await indexer.EnsureIndexAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
