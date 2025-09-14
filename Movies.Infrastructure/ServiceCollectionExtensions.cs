using MovieSearch.Movies.Infrastructure.Elasticsearch;

namespace MovieSearch.Movies.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration config)
        {
            var options = new ElasticsearchOptions();
            config.GetSection("Elasticsearch").Bind(options);

            services.AddSingleton(options);
            services.AddHttpClient<ElasticsearchHttp>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                // If you enable security later, add headers/credentials here
            });
            services.AddSingleton<IElasticsearchIndexInitializer, ElasticsearchIndexInitializer>();
            return services;
        }
    }
}
