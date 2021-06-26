using Nest;

namespace OrchardCore.Search.Elastic
{
    /// <summary>
    /// Represents an <see cref="IAnalyzer"/> instance that is available in the system.
    /// </summary>
    public interface IElasticAnalyzer
    {
        string Name { get; }
        IAnalyzer CreateAnalyzer();
    }
}
