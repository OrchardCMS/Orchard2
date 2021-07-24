namespace OrchardCore.Forms.Drivers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Localization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OrchardCore.Forms.Helpers;
    using OrchardCore.Forms.Models;

    /// <summary>
    /// Defines the <see cref="ValidationRuleOptions" />.
    /// </summary>
    public class ValidationRuleOptions
    {
        /// <summary>
        /// Defines the ValidationRuleProviders.
        /// </summary>
        public List<ValidationRuleProvider> ValidationRuleProviders = new List<ValidationRuleProvider>();

        /// <summary>
        /// The GenerateDefaultValidationRules.
        /// </summary>
        /// <param name="S">The S<see cref="IStringLocalizer"/>.</param>
        public void GenerateDefaultValidationRules(IStringLocalizer S)
        {
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 0,
                    displayName: S["None"],
                    name: S["None"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: false,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return true;
                    }
                  )
                );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 1,
                    displayName: S["Contains"],
                    name: S["Contains"],
                    isShowOption: true,
                    optionPlaceHolder: S["string to compare with input"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return input.Contains(option, StringComparison.InvariantCulture);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 2,
                    displayName: S["Equals"],
                    name: S["Equals"],
                    isShowOption: true,
                    optionPlaceHolder: S["string to compare with input"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return input.Equals(option, StringComparison.InvariantCultureIgnoreCase);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 3,
                    displayName: S["Is After"],
                    name: S["IsAfter"],
                    isShowOption: true,
                    optionPlaceHolder: S["2020-03-03"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return ValidationRuleHelpers.CompareDatetime(input, option, ">");
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 4,
                    displayName: S["Is Before"],
                    name: S["IsBefore"],
                    isShowOption: true,
                    optionPlaceHolder: S["2020-03-03"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return ValidationRuleHelpers.CompareDatetime(input, option, "<");
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 5,
                    displayName: S["Is Boolean"],
                    name: S["IsBoolean"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return Boolean.TryParse(input, out _);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 6,
                    displayName: S["Is ByteLength"],
                    name: S["IsByteLength"],
                    isShowOption: true,
                    optionPlaceHolder: S["{&quot;min&quot; : 0, &quot;max&quot; : 20}"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return ValidationRuleHelpers.ValidateLength(Encoding.UTF8.GetByteCount(input), option);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 7,
                    displayName: S["Is Date"],
                    name: S["IsDate"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return DateTime.TryParse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out _);

                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 8,
                    displayName: S["Is Decimal"],
                    name: S["IsDecimal"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return ValidationRuleHelpers.ValidateIs<decimal>(input);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 9,
                    displayName: S["Is Divisible By"],
                    name: S["IsDivisibleBy"],
                    isShowOption: true,
                    optionPlaceHolder: S["3"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        var result = false;
                        if (Single.TryParse(input, out var originalNumber) && Int32.TryParse(option, out var divisor))
                        {
                            if (divisor != 0) result = originalNumber % divisor == 0;
                        }
                        return result;
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 10,
                    displayName: S["Is Empty"],
                    name: S["IsEmpty"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return String.IsNullOrEmpty(input);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 11,
                    displayName: S["Is Float"],
                    name: S["IsFloat"],
                    isShowOption: false,
                    optionPlaceHolder: S["{ &quot; min & quot; : 7.22, &quot; max & quot; : 9.55}"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        var result = false;
                        if (Single.TryParse(input, out var original))
                        {
                            float min;
                            var obj = JToken.Parse(option);
                            Single.TryParse(obj["max"]?.ToString(), out var max);
                            Single.TryParse(obj["min"]?.ToString(), out min);
                            if (original >= min && (max == 0 || original <= max)) result = true;
                        }
                        return result;
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 12,
                    displayName: S["Is Int"],
                    name: S["IsInt"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return ValidationRuleHelpers.ValidateIs<int>(input);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 13,
                    displayName: S["Is JSON"],
                    name: S["IsJSON"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        var result = false;
                        if (!String.IsNullOrWhiteSpace(input))
                        {
                            var value = input.Trim();
                            if ((value.StartsWith("{") && value.EndsWith("}")) || // For object.
                                (value.StartsWith("[") && value.EndsWith("]"))) // For array.
                            {
                                try
                                {
                                    var ob = JToken.Parse(value);
                                    result = true;
                                }
                                catch (JsonReaderException)
                                {

                                }
                            }
                        }
                        return result;
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 14,
                    displayName: S["Is Length"],
                    name: S["IsLength"],
                    isShowOption: true,
                    optionPlaceHolder: S["{&quot;min&quot; : 7.22, &quot;max&quot; : 20.0}"],
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        return ValidationRuleHelpers.ValidateLength(input.Length, option);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 15,
                    displayName: S["Is Numeric"],
                    name: S["IsNumeric"],
                    isShowOption: false,
                    optionPlaceHolder: String.Empty,
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        var exp = @"^[0-9]+$";
                        return Regex.IsMatch(input, exp);
                    }
                )
            );
            ValidationRuleProviders.Add(
                new ValidationRuleProvider(
                    index: 16,
                    displayName: S["Matches"],
                    name: S["Matches"],
                    isShowOption: true,
                    optionPlaceHolder: "^\\\\d{n}$",
                    isShowErrorMessage: true,
                    validateInputByRuleAsync: (option, input) =>
                    {
                        var result = false;
                        if (!string.IsNullOrEmpty(option))
                        {
                            option = option.Replace("|-BackslashPlaceholder-|", "\\");
                            result = Regex.IsMatch(input, option);
                        }
                        return result;
                    }
                )
            );
        }
    }
}
