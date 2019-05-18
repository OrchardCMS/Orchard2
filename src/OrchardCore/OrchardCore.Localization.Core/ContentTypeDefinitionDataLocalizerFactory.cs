using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;

namespace OrchardCore.Localization
{
    public class ContentTypeDefinitionDataLocalizerFactory : IDataLocalizerFactory
    {
        private readonly ConcurrentDictionary<string, ContentTypeDefinitionDataLocalizer> _localizerCache = new ConcurrentDictionary<string, ContentTypeDefinitionDataLocalizer>();
        private readonly IContentDefinitionStore _contentDefinitionStore;
        private readonly bool _fallBackToParentUICultures;
        private readonly ILoggerFactory _loggerFactory;

        public ContentTypeDefinitionDataLocalizerFactory(
            IHttpContextAccessor httpContextAccessor,
            IOptions<RequestLocalizationOptions> requestLocalizationOptions,
            ILoggerFactory loggerFactory)
        {
            _contentDefinitionStore = httpContextAccessor?.HttpContext.RequestServices.GetService<IContentDefinitionStore>() ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _fallBackToParentUICultures = requestLocalizationOptions?.Value.FallBackToParentUICultures ?? throw new ArgumentNullException(nameof(requestLocalizationOptions));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IDataLocalizer Create()
        {
            var culture = CultureInfo.CurrentUICulture;
            IEnumerable<CultureDictionaryRecord> resources = null;

            return _localizerCache.GetOrAdd($"C={culture.Name}", _ =>
            {
                var dictionary = new CultureDictionary(culture.Name, null);
                resources = _fallBackToParentUICultures
                    ? GetResourcesFromCultureHierarchy(culture).GetAwaiter().GetResult()
                    : GetResources().GetAwaiter().GetResult();
                dictionary.MergeTranslations(resources);

                return new ContentTypeDefinitionDataLocalizer(dictionary, _loggerFactory.CreateLogger<DataLocalizer>());
            });
        }

        private async Task<IEnumerable<CultureDictionaryRecord>> GetResourcesFromCultureHierarchy(CultureInfo culture)
        {
            var currentCulture = culture;
            var records = new List<CultureDictionaryRecord>();
            var contentDefinition = await _contentDefinitionStore.LoadContentDefinitionAsync();

            do
            {
                var cultureResources = contentDefinition.ContentTypeDefinitionRecords.Select(r => new
                {
                    Key = r.DisplayName.Default,
                    Value = r.DisplayName.GetValueOrDefault(currentCulture.Name)
                });

                if (cultureResources != null)
                {
                    foreach (var resource in cultureResources)
                    {
                        var oldResurce = records.FirstOrDefault(r => r.Key == resource.Key);

                        if (oldResurce != null && oldResurce.Key != oldResurce.Translations.First())
                        {
                            // Don't override the translated resource in the parent culture(s)
                            continue;
                        }

                        records.Add(new CultureDictionaryRecord(resource.Key, new string[] { resource.Value }));
                    }
                }

                currentCulture = currentCulture.Parent;
            }
            while (currentCulture != currentCulture.Parent);

            return records;
        }

        private async Task<IEnumerable<CultureDictionaryRecord>> GetResources()
        {
            var contentDefinition = await _contentDefinitionStore.LoadContentDefinitionAsync();

            return contentDefinition.ContentTypeDefinitionRecords
                .Select(r => new CultureDictionaryRecord(r.DisplayName.Default, new string[] { r.DisplayName }));
        }
    }
}