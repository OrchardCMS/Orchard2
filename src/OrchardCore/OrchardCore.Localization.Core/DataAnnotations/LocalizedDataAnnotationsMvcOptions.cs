using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace OrchardCore.Localization.DataAnnotations
{
    internal class LocalizedDataAnnotationsMvcOptions : IConfigureOptions<MvcOptions>
    {
        private readonly IStringLocalizerFactory _stringLocalizerFactory;

        public LocalizedDataAnnotationsMvcOptions(IStringLocalizerFactory stringLocalizerFactory)
        {
            _stringLocalizerFactory = stringLocalizerFactory;
        }

        public void Configure(MvcOptions options)
        {
            var localizer = _stringLocalizerFactory.Create(typeof(LocalizedDataAnnotationsMvcOptions));

            options.ModelMetadataDetailsProviders.Add(new LocalizedValidationMetadataProvider(localizer));
        }
    }
}
