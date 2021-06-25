using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrchardCore.Localization;
using OrchardCore.Localization.Data;
using Xunit;

namespace OrchardCore.Tests.Localization
{
    public class DataLocalizerFactoryTests
    {
        private static readonly PluralizationRuleDelegate _noPluralRule = n => 0;

        private readonly Mock<ILocalizationManager> _localizationManagerMock;

        public DataLocalizerFactoryTests()
        {
            _localizationManagerMock = new Mock<ILocalizationManager>();
        }

        [Fact]
        public void CreateDataLocalizer()
        {
            // Arrange
            SetupDictionary("fr", new[] {
                new CultureDictionaryRecord("Hello", null, new[] { "Bonjour" })
            });

            var requestlocalizationOptions = Options.Create(new RequestLocalizationOptions { FallBackToParentUICultures = true });
            var loggerMock = new Mock<ILogger<DataLocalizerFactory>>();
            var localizerFactory = new DataLocalizerFactory(_localizationManagerMock.Object, requestlocalizationOptions, loggerMock.Object);

            // Act
            var localizer = localizerFactory.Create();

            CultureInfo.CurrentUICulture = new CultureInfo("fr");

            // Assert
            Assert.NotNull(localizer);
            Assert.Single(localizer.GetAllStrings());
        }

        private void SetupDictionary(string cultureName, IEnumerable<CultureDictionaryRecord> records)
        {
            var dictionary = new CultureDictionary(cultureName, _noPluralRule);
            dictionary.MergeTranslations(records);

            _localizationManagerMock.Setup(o => o.GetDictionary(It.Is<CultureInfo>(c => c.Name == cultureName))).Returns(dictionary);
        }
    }
}
