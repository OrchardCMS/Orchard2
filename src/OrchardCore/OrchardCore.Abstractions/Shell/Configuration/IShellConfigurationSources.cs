using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace OrchardCore.Environment.Shell.Configuration
{
    public interface IShellConfigurationSources
    {
        Task AddSourcesAsync(string tenant, IConfigurationBuilder builder);
        Task SaveAsync(string tenant, IDictionary<string, string> data);
        Task DeleteAsync(string tenant);
    }

    public static class ShellConfigurationSourcesExtensions
    {
        public static async Task<IConfigurationBuilder> AddSourcesAsync(this IConfigurationBuilder builder, string tenant, IShellConfigurationSources sources)
        {
            await sources.AddSourcesAsync(tenant, builder);
            return builder;
        }
    }
}
