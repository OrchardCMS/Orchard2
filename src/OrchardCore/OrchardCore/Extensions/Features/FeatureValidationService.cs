using System;
using System.IO.Enumeration;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace OrchardCore.Environment.Extensions.Features
{
    public class FeatureValidationService : IFeatureValidationService
    {
        private static Dictionary<string, Func<string, string, (bool isMatch, bool isAllowed)>> RuleSet = new Dictionary<string, Func<string, string, (bool, bool)>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Include",
                (expression, name) =>
                {
                    if (FileSystemName.MatchesSimpleExpression(expression, name))
                    {
                        return (true, true);
                    }
                    return (false, false);
                }
            },
            {
                "Exclude",
                (expression, name) =>
                {
                    if (FileSystemName.MatchesSimpleExpression(expression, name))
                    {
                        return (true, false);
                    }

                    return (false, false);
                }
            }
        };

        private readonly IExtensionManager _extensionManager;
        private readonly FeatureOptions _featureOptions;
        private readonly Dictionary<string, bool> _allowed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private List<FeatureOption> _sorted;

        public FeatureValidationService(IExtensionManager extensionManager, IOptions<FeatureOptions> featureOptions)
        {
            _extensionManager = extensionManager;
            _featureOptions = featureOptions.Value;
        }

        public bool IsFeatureValid(string id)
        {
            // Exclude rules are applied first, then Include rules are able to override an exclusion.
            _sorted ??= _featureOptions.Rules.GroupBy(x => x.Rule + x.Expression).Select(x => x.First()).OrderBy(x => x.Rule).ToList();
            var isAllowed = true;
            isAllowed = IsAllowed(id);
            if (!isAllowed)
            {
                return false;
            }

            var dependencies = _extensionManager.GetFeatureDependencies(id);

            foreach(var dependency in dependencies)
            {
                isAllowed = IsAllowed(dependency.Id);
                if (!isAllowed)
                {
                    return false;
                }
            }

            return isAllowed;
        }

        private bool IsAllowed(string id)
        {
            if (!_allowed.TryGetValue(id, out var isAllowed))
            {
                isAllowed = true;
                foreach(var rule in _sorted)
                {
                    if (RuleSet.TryGetValue(rule.Rule, out var ruleSet))
                    {
                        // Does rule match?
                        var result = ruleSet(rule.Expression, id);
                        if (result.isMatch)
                        {
                            isAllowed = result.isAllowed;
                        }
                    }
                }
                _allowed[id] = isAllowed;
            }

            return isAllowed;
        }
    }
}
