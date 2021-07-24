using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace OrchardCore.Localization.PortableObject
{
    /// <summary>
    /// Represents <see cref="IPluralStringLocalizer"/> for portable objects.
    /// </summary>
    public class PortableObjectStringLocalizer : IPluralStringLocalizer
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly bool _fallBackToParentCulture;
        private readonly ILogger _logger;
        private string _context;

        /// <summary>
        /// Creates a new instance of <see cref="PortableObjectStringLocalizer"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="localizationManager"></param>
        /// <param name="fallBackToParentCulture"></param>
        /// <param name="logger"></param>
        public PortableObjectStringLocalizer(
            string context,
            ILocalizationManager localizationManager,
            bool fallBackToParentCulture,
            ILogger logger)
        {
            _context = context;
            _localizationManager = localizationManager;
            _fallBackToParentCulture = fallBackToParentCulture;
            _logger = logger;
        }

        /// <inheritdocs />
        public virtual LocalizedString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var translation = GetTranslation(name, _context, CultureInfo.CurrentUICulture, null);

                return new LocalizedString(name, translation ?? name, translation == null);
            }
        }

        /// <inheritdocs />
        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var (translation, argumentsWithCount) = GetTranslation(name, arguments);
                var formatted = string.Format(translation.Value, argumentsWithCount);

                return new LocalizedString(name, formatted, translation.ResourceNotFound);
            }
        }

        /// <inheritdocs />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var culture = CultureInfo.CurrentUICulture;

            return includeParentCultures
                ? GetAllStringsFromCultureHierarchy(culture)
                : GetAllStrings(culture);
        }

        [Obsolete("This method will be removed in the upcoming ASP.NET Core major release.")]
        public IStringLocalizer WithCulture(CultureInfo culture) => this;

        /// <inheritdocs />
        public virtual (LocalizedString, object[]) GetTranslation(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Check if a plural form is called, which is when the only argument is of type PluralizationArgument
            if (arguments.Length == 1 && arguments[0] is PluralizationArgument pluralArgument)
            {
                var translation = GetTranslation(name, _context, CultureInfo.CurrentUICulture, pluralArgument.Count);

                object[] argumentsWithCount;

                if (pluralArgument.Arguments.Length > 0)
                {
                    argumentsWithCount = new object[pluralArgument.Arguments.Length + 1];
                    argumentsWithCount[0] = pluralArgument.Count;
                    Array.Copy(pluralArgument.Arguments, 0, argumentsWithCount, 1, pluralArgument.Arguments.Length);
                }
                else
                {
                    argumentsWithCount = new object[] { pluralArgument.Count };
                }

                translation = translation ?? GetTranslation(pluralArgument.Forms, CultureInfo.CurrentUICulture, pluralArgument.Count);

                return (new LocalizedString(name, translation, translation == null), argumentsWithCount);
            }
            else
            {
                var translation = this[name];
                return (new LocalizedString(name, translation, translation.ResourceNotFound), arguments);
            }
        }

        private IEnumerable<LocalizedString> GetAllStrings(CultureInfo culture)
        {
            var dictionary = _localizationManager.GetDictionary(culture);

            foreach (var translation in dictionary.Translations)
            {
                yield return new LocalizedString(translation.Key, translation.Value.FirstOrDefault());
            }
        }

        private IEnumerable<LocalizedString> GetAllStringsFromCultureHierarchy(CultureInfo culture)
        {
            var currentCulture = culture;
            var allLocalizedStrings = new List<LocalizedString>();

            do
            {
                var localizedStrings = GetAllStrings(currentCulture);

                if (localizedStrings != null)
                {
                    foreach (var localizedString in localizedStrings)
                    {
                        if (!allLocalizedStrings.Any(ls => ls.Name == localizedString.Name))
                        {
                            allLocalizedStrings.Add(localizedString);
                        }
                    }
                }

                currentCulture = currentCulture.Parent;
            } while (currentCulture != currentCulture.Parent);

            return allLocalizedStrings;
        }

        protected string GetTranslation(string[] pluralForms, CultureInfo culture, int? count)
        {
            var dictionary = _localizationManager.GetDictionary(culture);

            var pluralForm = count.HasValue ? dictionary.PluralRule(count.Value) : 0;

            if (pluralForm >= pluralForms.Length)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Plural form '{PluralForm}' doesn't exist in values provided by the 'IStringLocalizer.Plural' method. Provided values: {PluralForms}", pluralForm, String.Join(", ", pluralForms));
                }

                // Use the latest available form
                return pluralForms[pluralForms.Length - 1];
            }

            return pluralForms[pluralForm];
        }

        protected string GetTranslation(string name, string context, CultureInfo culture, int? count)
        {
            string translation = null;
            try
            {
                if (_fallBackToParentCulture)
                {
                    do
                    {
                        if (ExtractTranslation() != null)
                        {
                            break;
                        }

                        culture = culture.Parent;
                    }
                    while (culture != CultureInfo.InvariantCulture);
                }
                else
                {
                    ExtractTranslation();
                }

                string ExtractTranslation()
                {
                    var dictionary = _localizationManager.GetDictionary(culture);

                    if (dictionary != null)
                    {
                        // Extract translation with context
                        var key = CultureDictionaryRecord.GetKey(name, context);
                        translation = dictionary[key, count];

                        if (context != null && translation == null)
                        {
                            // Extract translation without context
                            key = CultureDictionaryRecord.GetKey(name, null);
                            translation = dictionary[key, count];
                        }
                    }

                    return translation;
                }
            }
            catch (PluralFormNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
            }

            return translation;
        }
    }
}
