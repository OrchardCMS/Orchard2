using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrchardCore.Environment.Shell
{
    public class SingleShellSettingsManager : IShellSettingsManager
    {
        public ShellSettings CreateDefaultSettings()
        {
            return new ShellSettings()
            {
                Name = ShellHelper.DefaultShellName,
                State = Models.TenantState.Running
            };
        }

        public Task<IEnumerable<ShellSettings>> LoadSettingsAsync()
        {
            return Task.FromResult((new ShellSettings[] { CreateDefaultSettings() }).AsEnumerable());
        }

        public Task<IEnumerable<string>> LoadSettingsNamesAsync()
        {
            return Task.FromResult((new string[] { ShellHelper.DefaultShellName }).AsEnumerable());
        }

        public Task<ShellSettings> LoadSettingsAsync(string tenant) => Task.FromResult(CreateDefaultSettings());

        public Task SaveSettingsAsync(ShellSettings shellSettings) => Task.CompletedTask;

        public Task DeleteSettingsAsync(ShellSettings shellSettings) => Task.CompletedTask;
    }
}
