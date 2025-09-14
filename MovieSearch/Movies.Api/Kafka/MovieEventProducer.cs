using Confluent.Kafka;
using System.Text.Json;

namespace MovieSearch.Movies.Api.Kafka
{
    public interface IMovieEventProducer
    {
        Task ProduceUpsertAsync(object evt, CancellationToken ct = default);
    }


    public sealed class MovieEventProducer : IMovieEventProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;


        public MovieEventProducer(IConfiguration cfg)
        {
            var section = cfg.GetSection("Kafka");
            _topic = section.GetValue<string>("Topic") ?? "movies.upserted";
            var conf = new ProducerConfig
            {
                BootstrapServers = section.GetValue<string>("BootstrapServers"),
            };
            var proto = section.GetValue<string>("SecurityProtocol");
            if (!string.IsNullOrWhiteSpace(proto) && !proto.Equals("PLAINTEXT", StringComparison.OrdinalIgnoreCase))
            {
                conf.SecurityProtocol = Enum.Parse<SecurityProtocol>(proto, true);
                conf.SaslMechanism = Enum.Parse<SaslMechanism>(section.GetValue<string>("SaslMechanism") ?? "PLAIN", true);
                conf.SaslUsername = section.GetValue<string>("SaslUsername");
                conf.SaslPassword = section.GetValue<string>("SaslPassword");
            }
            _producer = new ProducerBuilder<string, string>(conf).Build();
        }


        public async Task ProduceUpsertAsync(object evt, CancellationToken ct = default)
        {
            var key = Guid.NewGuid().ToString("N");
            var val = JsonSerializer.Serialize(evt);
            var dr = await _producer.ProduceAsync(_topic, new Message<string, string> { Key = key, Value = val }, ct);
            // Optional: log dr.Status or dr.Offset
        }


        public void Dispose() => _producer?.Dispose();
    }
}
