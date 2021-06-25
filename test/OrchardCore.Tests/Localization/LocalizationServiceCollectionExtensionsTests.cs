using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Localization;
using Xunit;

namespace OrchardCore.Tests.Localization
{
    public class LocalizationServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDataLocaliztion_RegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddMemoryCache();

            // Act
            services.AddDataLocalization();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var translationProvider = serviceProvider.GetService<ITranslationProvider>();
            var localizationManager = serviceProvider.GetService<ILocalizationManager>();
            var dataLocalizerFactory = serviceProvider.GetService<IDataLocalizerFactory>();
            var dataLocalizer = serviceProvider.GetService<IDataLocalizer>();

            Assert.NotNull(translationProvider);
            Assert.NotNull(localizationManager);
            Assert.NotNull(dataLocalizerFactory);
            Assert.NotNull(dataLocalizer);
        }
    }
}
