using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MovieSearch.Movies.Application.Interfaces;
using MovieSearch.Movies.Application.Services;
using MovieSearch.Movies.Infrastructure;
using MovieSearch.Movies.Infrastructure.Elasticsearch;
using MovieSearch.Movies.Domain;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ES + App services (only needed when PrintOnly=false)
builder.Services.AddElasticsearch(builder.Configuration);
builder.Services.AddSingleton<IMovieSearchService, MovieSearchService>();

// Kafka consumer
builder.Services.AddSingleton(sp =>
{
    var cfg = builder.Configuration.GetSection("Kafka");
    var conf = new ConsumerConfig
    {
        BootstrapServers = cfg.GetValue<string>("BootstrapServers"),
        GroupId = cfg.GetValue<string>("GroupId") ?? "movies-indexer",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true
    };
    var proto = cfg.GetValue<string>("SecurityProtocol");
    if (!string.IsNullOrWhiteSpace(proto) && !proto.Equals("PLAINTEXT", StringComparison.OrdinalIgnoreCase))
    {
        conf.SecurityProtocol = Enum.Parse<SecurityProtocol>(proto, true);
        conf.SaslMechanism = SaslMechanism.Plain;
        conf.SaslUsername = cfg.GetValue<string>("SaslUsername");
        conf.SaslPassword = cfg.GetValue<string>("SaslPassword");
    }
    return new ConsumerBuilder<string, string>(conf).Build();
});

builder.Services.AddHostedService<KafkaMovieIndexer>();

var host = builder.Build();
await host.RunAsync();

public class KafkaMovieIndexer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IConfiguration _cfg;
    private readonly ILogger<KafkaMovieIndexer> _log;
    private readonly IMovieSearchService _svc;
    private readonly bool _printOnly;

    public KafkaMovieIndexer(IConsumer<string, string> consumer,
                             IConfiguration cfg,
                             ILogger<KafkaMovieIndexer> log,
                             IMovieSearchService svc)
    {
        _consumer = consumer;
        _cfg = cfg;
        _log = log;
        _svc = svc;
        _printOnly = _cfg.GetValue<bool>("Worker:PrintOnly");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topic = _cfg.GetValue<string>("Kafka:Topic") ?? "movies.upserted";
        _log.LogInformation("Starting worker. Subscribing to topic: {Topic}", topic);
        _consumer.Subscribe(topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = _consumer.Consume(stoppingToken);
                if (cr == null) continue;

                _log.LogInformation("Message received. Key={Key} Offset={Offset}", cr.Message.Key, cr.Offset);
                _log.LogInformation("Payload: {Payload}", cr.Message.Value);

                if (_printOnly) continue; // observe messages first if you want

                using var doc = JsonDocument.Parse(cr.Message.Value);
                if (doc.RootElement.TryGetProperty("payload", out var payload))
                {
                    var movie = payload.Deserialize<Movie>();
                    if (movie != null)
                    {
                        await _svc.IndexAsync(movie);
                        _log.LogInformation("Indexed movie via Kafka: {Id} - {Title}", movie.Id, movie.Title);
                    }
                    else
                    {
                        _log.LogWarning("Payload could not be deserialized to Movie.");
                    }
                }
                else
                {
                    _log.LogWarning("Message missing 'payload' property.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error consuming message");
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Shutting down worker...");
        _consumer.Close();
        _consumer.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
