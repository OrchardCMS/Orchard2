using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrchardCore.Localization.Data
{
    public class DataLocalizerFactory : IDataLocalizerFactory
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly bool _fallBackToParentCulture;
        private readonly ILogger _logger;

        public DataLocalizerFactory(
            ILocalizationManager localizationManager,
            IOptions<RequestLocalizationOptions> requestLocalizationOptions,
            ILogger<DataLocalizerFactory> logger)
        {
            _localizationManager = localizationManager;
            _fallBackToParentCulture = requestLocalizationOptions.Value.FallBackToParentUICultures;
            _logger = logger;
        }

        public IDataLocalizer Create()
            => new DataLocalizer(_localizationManager, _fallBackToParentCulture, _logger);
    }
}
