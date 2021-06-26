using Microsoft.Extensions.DependencyInjection;


namespace OrchardCore.Search.Elastic
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Lucene queries services.
        /// </summary>
        public static IServiceCollection AddElasticQueries(this IServiceCollection services)
        {
            services.AddScoped<IElasticQueryService, ElasticQueryService>();
            return services;
        }
    }
}
