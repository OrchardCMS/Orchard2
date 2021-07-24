using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.Forms.Models;

namespace OrchardCore.Forms.Helpers
{
    public class ValidationRuleHelpers
    {
        private readonly IContentManager _contentManager;

        public ValidationRuleHelpers(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }
        public static bool ValidateLength(int len, string option)
        {
            try
            {
                var min = 0;
                var max = Int32.MaxValue;
                var obj = JToken.Parse(option);
                Int32.TryParse(obj["max"]?.ToString(), out max);
                Int32.TryParse(obj["min"]?.ToString(), out min);
                if (len >= min && (max == 0 || len <= max)) return true;
                return false;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        public static bool ValidateIs<T>(string value)
        {
            if (value == null) return false;
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
            try
            {
                var result = (T)converter.ConvertFromString(value.ToString());
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool CompareDatetime(string input, string option, string symbol)
        {
            var originResult = DateTime.TryParse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out var originDate);
            var compareResult = DateTime.TryParse(option, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out var compareDate);
            if (symbol == ">") return originResult && compareResult && originDate > compareDate;
            return originResult && compareResult && originDate < compareDate;
        }
        public async Task<List<ValidationRuleAspect>> GetValidationRuleAspects(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId);
            var validationRuleAspectList = await _contentManager.PopulateAspectAsync<List<ValidationRuleAspect>>(contentItem);
            return validationRuleAspectList;
        }
    }
}
