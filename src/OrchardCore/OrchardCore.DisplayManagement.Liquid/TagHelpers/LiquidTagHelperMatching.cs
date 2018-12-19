using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace OrchardCore.DisplayManagement.Liquid.TagHelpers
{
    public class LiquidTagHelperMatching
    {
        private const string AspPrefix = "asp-";
        public readonly static LiquidTagHelperMatching None = new LiquidTagHelperMatching();
        public readonly IEnumerable<TagMatchingRuleDescriptor> _rules = Enumerable.Empty<TagMatchingRuleDescriptor>();

        public LiquidTagHelperMatching() { }

        public LiquidTagHelperMatching(string name, string assemblyName, IEnumerable<TagMatchingRuleDescriptor> tagMatchingRules)
        {
            Name = name;
            AssemblyName = assemblyName;
            _rules = tagMatchingRules.ToArray();
        }

        public string Name { get; } = String.Empty;
        public string AssemblyName { get; } = String.Empty;

        public bool Match(string helper, IEnumerable<string> arguments)
        {

            return _rules.Any(rule =>
            {
                // Does it match the required tag name
                if (rule.TagName != "*" && !String.Equals(rule.TagName, helper, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Does it expect any specific attribute?
                if (!rule.Attributes.Any())
                {
                    return true;
                }

                // Are all required attributes present?
                var allRequired = rule.Attributes.All(attr => arguments.Any(name =>
                {
                    // Exact match
                    if (String.Equals(name, attr.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Check by replacing all '_' with '-', e.g. asp_src will map to asp-src
                    name = name.Replace('_', '-');

                    if (attr.Name.StartsWith(AspPrefix) && String.Equals(name, attr.Name.Substring(AspPrefix.Length), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (String.Equals(name, attr.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                }));

                if (allRequired)
                {
                    return true;
                }
    
                return false;
            });
        }
    }
}