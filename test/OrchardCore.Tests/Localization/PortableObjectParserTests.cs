using System.IO;
using System.Linq;
using OrchardCore.Localization;
using OrchardCore.Localization.PortableObject;
using Xunit;

namespace OrchardCore.Tests.Localization
{
    public class PortableObjectParserTests
    {
        [Fact]
        public void ParseRetursSimpleEntry()
        {
            // msgid "Unknown system error"
            // msgstr "Error desconegut del sistema"
            var entries = ParseText("SimpleEntry");

            Assert.Equal("Unknown system error", entries[0].Key);
            Assert.Equal("Error desconegut del sistema", entries[0].Translations[0]);
        }

        [Fact]
        public void ParseIgnoresEntryWithoutTranslation()
        {
            // "msgid "Unknown system error"
            // "msgstr ""
            var entries = ParseText("EntryWithoutTranslation");

            Assert.Empty(entries);
        }

        [Fact]
        public void ParseIgnoresPoeditHeader()
        {
            // # Translation of kstars.po into Spanish.
            // # This file is distributed under the same license as the kdeedu package.
            // # Pablo de Vicente <pablo@foo.com>, 2005, 2006, 2007, 2008.
            // # Eloy Cuadra <eloy@bar.net>, 2007, 2008.
            // msgid ""
            // msgstr ""
            // "Project-Id-Version: kstars\n"
            // "Report-Msgid-Bugs-To: http://bugs.kde.org\n"
            // "POT-Creation-Date: 2008-09-01 09:37+0200\n"
            // "PO-Revision-Date: 2008-07-22 18:13+0200\n"
            // "Last-Translator: Eloy Cuadra <eloy@bar.net>\n"
            // "Language-Team: Spanish <kde-l10n-es@kde.org>\n"
            // "MIME-Version: 1.0\n"
            // "Content-Type: text/plain; charset=UTF-8\n"
            // "Content-Transfer-Encoding: 8bit\n"
            // "Plural-Forms: nplurals=2; plural=n != 1;\n"

            // msgid "Unknown system error"
            // msgstr "Error desconegut del sistema"
            var entries = ParseText("PoeditHeader");

            Assert.True(entries.Count() == 1);
            Assert.True(entries[0].Translations.Count() == 1);
        }

        [Fact]
        public void ParseSetsContext()
        {
            // msgctxt "OrchardCore.Localization"
            // msgid "Unknown system error"
            // msgstr "Error desconegut del sistema"
            var entries = ParseText("EntryWithContext");

            Assert.Equal("OrchardCore.Localization|Unknown system error", entries[0].Key, ignoreCase: true);
        }

        [Fact]
        public void ParseIgnoresComments()
        {
            // # translator-comments
            // #. extracted-comments
            // #: reference…
            // #, flag

            // #| msgctxt previous-context
            // #| msgid previous-untranslated-string
            // msgctxt "OrchardCore.Localization"
            // msgid "Unknown system error"
            // msgstr "Error desconegut del sistema"

            var entries = ParseText("EntryWithComments");

            Assert.Equal("OrchardCore.Localization|Unknown system error", entries[0].Key, ignoreCase: true);
            Assert.Equal("Error desconegut del sistema", entries[0].Translations[0]);
        }

        [Fact]
        public void ParseOnlyTrimsLeadingAndTrailingQuotes()
        {
            // msgid "\"{0}\""
            // msgstr "\"{0}\""

            var entries = ParseText("EntryWithQuotes");

            Assert.Equal("\"{0}\"", entries[0].Key);
            Assert.Equal("\"{0}\"", entries[0].Translations[0]);
        }

        //[Fact]
        //public void ParseHandleUnclosedQuote()
        //{
        //    // msgctxt "
        //    // msgid "Foo \"{0}\""
        //    // msgstr "Foo \"{0}\""

        //    var entries = ParseText("EntryWithUnclosedQuote");

        //    Assert.Equal("Foo \"{0}\"", entries[0].Key);
        //}

        [Fact]
        public void ParseHandlesMultilineEntry()
        {
            // msgid ""
            // "Here is an example of how one might continue a very long string\n"
            // "for the common case the string represents multi-line output."
            // msgstr ""
            // "Here is an example of how one might continue a very long translation\n"
            // "for the common case the string represents multi-line output."

            var entries = ParseText("EntryWithMultilineText");

            Assert.Equal("Here is an example of how one might continue a very long string\nfor the common case the string represents multi-line output.", entries[0].Key);
            Assert.Equal("Here is an example of how one might continue a very long translation\nfor the common case the string represents multi-line output.", entries[0].Translations[0]);
        }

        [Fact]
        public void ParsePreservesEscapedCharacters()
        {
            // msgid "Line:\t\"{0}\"\n"
            // msgstr "Line:\t\"{0}\"\n"

            var entries = ParseText("EntryWithEscapedCharacters");

            Assert.Equal("Line:\t\"{0}\"\n", entries[0].Key);
            Assert.Equal("Line:\t\"{0}\"\n", entries[0].Translations[0]);
        }

        [Fact]
        public void ParseReadsPluralTranslations()
        {
            // msgid "book"
            // msgid_plural "books"
            // msgstr[0] "kniha"
            // msgstr[1] "knihy"
            // msgstr[2] "knih"

            var entries = ParseText("EntryWithPlural");

            Assert.Equal("book", entries[0].Key);
            Assert.Equal("kniha", entries[0].Translations[0]);
            Assert.Equal("knihy", entries[0].Translations[1]);
            Assert.Equal("knih", entries[0].Translations[2]);
        }

        [Fact]
        public void ParseReadsMultipleEntries()
        {
            // #. "File {0} does not exist"
            // msgctxt "OrchardCore.FileSystems.Media.FileSystemStorageProvider"
            // msgid "File {0} does not exist"
            // msgstr "Soubor {0} neexistuje"

            // #. "Directory {0} does not exist"
            // msgctxt "OrchardCore.FileSystems.Media.Directory"
            // msgid "Directory {0} does not exist"
            // msgstr "Složka {0} neexistuje"

            var entries = ParseText("MultipleEntries");

            Assert.Equal(2, entries.Length);

            Assert.Equal("OrchardCore.FileSystems.Media.FileSystemStorageProvider|File {0} does not exist", entries[0].Key, ignoreCase: true);
            Assert.Equal("Soubor {0} neexistuje", entries[0].Translations[0]);

            Assert.Equal("OrchardCore.FileSystems.Media.Directory|Directory {0} does not exist", entries[1].Key, ignoreCase: true);
            Assert.Equal("Složka {0} neexistuje", entries[1].Translations[0]);
        }

        private CultureDictionaryRecord[] ParseText(string resourceName)
        {
            var parser = new PortableObjectParser();
            var testAssembly = typeof(PortableObjectParserTests).Assembly;
            using (var resource = testAssembly.GetManifestResourceStream("OrchardCore.Tests.Localization.PoFiles." + resourceName + ".po"))
            {
                using (var reader = new StreamReader(resource))
                {
                    return parser.Parse(reader).ToArray();
                }
            }
        }
    }
}
