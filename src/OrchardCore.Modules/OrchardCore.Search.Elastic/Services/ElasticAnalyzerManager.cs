using System;
using System.Collections.Generic;
using Nest;
using Microsoft.Extensions.Options;

namespace OrchardCore.Search.Elastic.Services
{
    /// <summary>
    /// Coordinates <see cref="IElasticAnalyzer"/> implementations provided by <see cref="ElasticOptions"/>
    /// to return the list of all available <see cref="IElasticAnalyzer"/> objects.
    /// </summary>
    public class ElasticAnalyzerManager
    {
        private readonly Dictionary<string, IElasticAnalyzer> _analyzers;

        public ElasticAnalyzerManager(IOptions<ElasticOptions> options)
        {
            _analyzers = new Dictionary<string, IElasticAnalyzer>(StringComparer.OrdinalIgnoreCase);

            foreach (var analyzer in options.Value.Analyzers)
            {
                _analyzers[analyzer.Name] = analyzer;
            }
        }

        public IEnumerable<IElasticAnalyzer> GetAnalyzers()
        {
            return _analyzers.Values;
        }

        public IAnalyzer CreateAnalyzer(string name)
        {
            if (_analyzers.TryGetValue(name, out var analyzer))
            {
                return analyzer.CreateAnalyzer();
            }

            return null;
        }
    }
}
