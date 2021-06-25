using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using OrchardCore.Localization;
using OrchardCore.Localization.Data;
using Xunit;

namespace OrchardCore.Tests.Localization
{
    public class DataLocalizerTests
    {
        private static readonly PluralizationRuleDelegate _noPluralRule = n => 0;

        private readonly Mock<ILocalizationManager> _localizationManager;
        private readonly Mock<ILogger> _logger;

        public DataLocalizerTests()
        {
            _localizationManager = new Mock<ILocalizationManager>();
            _logger = new Mock<ILogger>();
        }

        [Fact]
        public void LocalizerReturnsTranslationsFromProvidedDictionary()
        {
            SetupDictionary("fr", new[] {
                new CultureDictionaryRecord("Hello", null, new[] { "Bonjour" })
            });

            var localizer = new DataLocalizer(_localizationManager.Object, true, _logger.Object);

            CultureInfo.CurrentUICulture = new CultureInfo("fr");

            var translation = localizer["Hello"];

            Assert.Equal("Bonjour", translation);
        }

        [Fact]
        public void LocalizerReturnsOriginalTextIfTranslationsDoesntExistInProvidedDictionary()
        {
            SetupDictionary("fr", new[] {
                new CultureDictionaryRecord("Hello", null, new[] { "Bonjour" })
            });

            var localizer = new DataLocalizer(_localizationManager.Object, true, _logger.Object);

            CultureInfo.CurrentUICulture = new CultureInfo("fr");

            var translation = localizer["Bye"];

            Assert.Equal("Bye", translation);
        }

        [Fact]
        public void LocalizerReturnsOriginalTextIfDictionaryIsEmpty()
        {
            SetupDictionary("fr", Array.Empty<CultureDictionaryRecord>());

            var localizer = new DataLocalizer(_localizationManager.Object, true, _logger.Object);

            CultureInfo.CurrentUICulture = new CultureInfo("fr");

            var translation = localizer["Hello"];

            Assert.Equal("Hello", translation);
        }

        [Fact]
        public void LocalizerFallbacksToParentCultureIfTranslationDoesntExistInSpecificCulture()
        {
            SetupDictionary("fr", new[] {
                new CultureDictionaryRecord("Hello", null, new[] { "Bonjour" })
            });
            SetupDictionary("fr-FR", new[] {
                new CultureDictionaryRecord("Bye", null, new[] { "au revoir" })
            });

            var localizer = new DataLocalizer(_localizationManager.Object, true, _logger.Object);

            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            var translation = localizer["Hello"];

            Assert.Equal("Bonjour", translation);
        }

        [Fact]
        public void LocalizerReturnsTranslationFromSpecificCultureIfItExists()
        {
            SetupDictionary("fr", new[] {
                new CultureDictionaryRecord("Hello", null, new[] { "Bonjour" })
            });
            SetupDictionary("fr-FR", new[] {
                new CultureDictionaryRecord("Bye", null, new[] { "au revoir" })
            });

            var localizer = new DataLocalizer(_localizationManager.Object, true, _logger.Object);

            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            var translation = localizer["Bye"];

            Assert.Equal("au revoir", translation);
        }

        [Fact]
        public void LocalizerReturnsFormattedTranslation()
        {
            SetupDictionary("cs", new[] {
                new CultureDictionaryRecord("The page (ID:{0}) was deleted.", null, new[] { "Stránka (ID:{0}) byla smazána." })
            });
            var localizer = new DataLocalizer(_localizationManager.Object, true, _logger.Object);

            CultureInfo.CurrentUICulture = new CultureInfo("cs");

            var translation = localizer["The page (ID:{0}) was deleted.", 1];

            Assert.Equal("Stránka (ID:1) byla smazána.", translation);
        }

        [Theory]
        [InlineData(false, "hello", "hello")]
        [InlineData(true, "hello", "مرحبا")]
        public void LocalizerFallBackToParentCultureIfFallBackToParentUICulturesIsTrue(bool fallBackToParentCulture, string resourceKey, string expected)
        {
            SetupDictionary("ar", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("hello", null, new[] { "مرحبا" })
            });
            SetupDictionary("ar-YE", Array.Empty<CultureDictionaryRecord>());
            var localizer = new DataLocalizer(_localizationManager.Object, fallBackToParentCulture, _logger.Object);
            CultureInfo.CurrentUICulture = new CultureInfo("ar-YE");
            var translation = localizer[resourceKey];

            Assert.Equal(expected, translation);
        }

        [Theory]
        [InlineData(false, new[] { "مدونة", "منتج" })]
        [InlineData(true, new[] { "مدونة", "منتج", "قائمة", "صفحة", "مقالة" })]
        public void LocalizerReturnsGetAllStrings(bool includeParentCultures, string[] expected)
        {
            SetupDictionary("ar", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("Blog", null, new[] { "مدونة" }),
                new CultureDictionaryRecord("Menu", null, new[] { "قائمة" }),
                new CultureDictionaryRecord("Page", null, new[] { "صفحة" }),
                new CultureDictionaryRecord("Article", null, new[] { "مقالة" })
            });
            SetupDictionary("ar-YE", new CultureDictionaryRecord[] {
                new CultureDictionaryRecord("Blog", null, new[] { "مدونة" }),
                new CultureDictionaryRecord("Product", null, new[] { "منتج" })
            });

            var localizer = new DataLocalizer(_localizationManager.Object, false, _logger.Object);
            CultureInfo.CurrentUICulture = new CultureInfo("ar-YE");
            var translations = localizer.GetAllStrings(includeParentCultures).Select(l => l.Value).ToArray();

            Assert.Equal(expected.Length, translations.Length);
        }

        private void SetupDictionary(string cultureName, IEnumerable<CultureDictionaryRecord> records)
        {
            var dictionary = new CultureDictionary(cultureName, _noPluralRule);
            dictionary.MergeTranslations(records);

            _localizationManager.Setup(o => o.GetDictionary(It.Is<CultureInfo>(c => c.Name == cultureName))).Returns(dictionary);
        }
    }
}
