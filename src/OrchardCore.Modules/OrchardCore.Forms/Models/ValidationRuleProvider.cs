using System;
using System.Collections.Generic;
using System.Text;

namespace OrchardCore.Forms.Models
{
    public class ValidationRuleProvider
    {
        public int PostionIndex { get; set; }
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public bool IsShowOption { get; set; }
        public string OptionPlaceHolder { get; set; }
        public bool IsShowErrorMessage  { get; set; }
        public readonly Func<string,string,bool> ValidateInputByRuleAsync;
        public ValidationRuleProvider(
            int index,
            string displayName,
            string name,
            bool isShowOption,
            string optionPlaceHolder,
            bool isShowErrorMessage,
            Func<string,string,bool> validateInputByRuleAsync
            )
        {
            PostionIndex = index;
            DisplayName = displayName;
            Name = name;
            IsShowOption = isShowOption;
            OptionPlaceHolder = optionPlaceHolder;
            IsShowErrorMessage = isShowErrorMessage;
            ValidateInputByRuleAsync = validateInputByRuleAsync;


        }
    }
}
