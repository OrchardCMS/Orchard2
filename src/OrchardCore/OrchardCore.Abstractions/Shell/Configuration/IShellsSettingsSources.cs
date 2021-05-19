using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace OrchardCore.Environment.Shell.Configuration
{
    public interface IShellsSettingsSources
    {
        Task AddSourcesAsync(IConfigurationBuilder builder);
        Task SaveAsync(string tenant, IDictionary<string, string> data);
        Task DeleteAsync(string tenant);
    }

    public static class ShellsSettingsSourcesExtensions
    {
        public static async Task<IConfigurationBuilder> AddSourcesAsync(this IConfigurationBuilder builder, IShellsSettingsSources sources)
        {
            await sources.AddSourcesAsync(builder);
            return builder;
        }
    }
}
