using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using OrchardCore.DataLocalization.Models;
using OrchardCore.DataLocalization.Services;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Localization
{
    public class TranslationsManagerTests
    {
        [Fact]
        public async Task GetTranslationsDocument()
        {
            var translationsManager = new Mock<ITranslationsManager>();
            translationsManager.Setup(tm => tm.GetTranslationsDocumentAsync())
                .ReturnsAsync(() => {
                    var translationsDocument = new TranslationsDocument();
                    foreach (var translation in GetTranslations())
                    {
                        var key = $"{translation.Context}:{translation.Key}";
                        translationsDocument.Translations.Add(key, translation);
                    }

                    return translationsDocument;
                });
            var translationsDocument = await translationsManager.Object.GetTranslationsDocumentAsync();

            Assert.NotNull(translationsDocument);
            Assert.Equal(4, translationsDocument.Translations.Count);
            var firstTranslation = translationsDocument.Translations.First();
            Assert.Equal("Article1", firstTranslation.Value.Key);
            Assert.Equal("es", firstTranslation.Value.Values.Last().Key);
            Assert.Equal("Artículo1", firstTranslation.Value.Values.Last().Value);
        }

        [Fact]
        public async Task GetTranslationsDocument_GroupByContext()
        {
            var translationsManager = new Mock<ITranslationsManager>();
            translationsManager.Setup(tm => tm.GetTranslationsDocumentAsync())
                .ReturnsAsync(() => {
                    var translationsDocument = new TranslationsDocument();
                    foreach (var translation in GetTranslations())
                    {
                        var key = $"{translation.Context}:{translation.Key}";
                        translationsDocument.Translations.Add(key, translation);
                    }

                    return translationsDocument;
                });
            var translationsDocument = await translationsManager.Object.GetTranslationsDocumentAsync();
            var groups = translationsDocument.Translations.GroupBy(t => t.Value.Context);

            Assert.Equal(2, groups.Count());
            Assert.StartsWith("Content Type", groups.First().Key);
            Assert.Equal(3, (int)groups.First().Count());
            Assert.StartsWith("Content Field", groups.Last().Key);
            Assert.Equal(1, (int)groups.Last().Count());
        }

        private IEnumerable<Translation> GetTranslations()
        {
            return new List<Translation>{
                new Translation {
                    Context = "Content Type",
                    Key = "Article1",
                    Values = new Dictionary<string, string> {
                        { "en", "Article1" },
                        { "fr", "Article1" },
                        { "es", "Artículo1" }
                    }
                },
                new Translation {
                    Context = "Content Type",
                    Key = "Article2",
                    Values = new Dictionary<string, string> {
                        { "en", "Article2" },
                        { "fr", "Article2" },
                        { "es", "" }
                    }
                },
                new Translation {
                    Context = "Content Type",
                    Key = "Article3",
                    Values = new Dictionary<string, string> {
                        { "en", "" },
                        { "fr", "" },
                        { "es", "Artículo3" }
                    }
                },
                new Translation {
                    Context = "Content Field",
                    Key = "Name",
                    Values = new Dictionary<string, string> {
                        { "en", "Name" },
                        { "fr", "Nome" },
                        { "es", "Nombre" }
                    }
                },
            };
        }
    }
}
