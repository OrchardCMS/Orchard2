using Microsoft.Extensions.DependencyInjection;
using Orchard.Lucene.QueryProviders;

namespace Orchard.Lucene
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Lucene queries services.
        /// </summary>
        public static IServiceCollection AddLuceneQueries(this IServiceCollection services)
        {
            services.AddScoped<IQueryDslBuilder, QueryDslBuilder>();

            services.AddSingleton<ILuceneQueryProvider, BooleanQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, FuzzyQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, MatchPhraseQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, MatchQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, PrefixQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, QueryStringQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, RangeQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, RegexpQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, RootQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, SimpleQueryStringQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, TermQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, TermsQueryProvider>();
            services.AddSingleton<ILuceneQueryProvider, WildcardQueryProvider>();
            return services;
        }
    }
}