using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Environment.Shell.Configuration
{
    public class ShellsSettingsSources : IShellsSettingsSources
    {
        private readonly string _tenants;

        public ShellsSettingsSources(IOptions<ShellOptions> shellOptions)
        {
            _tenants = Path.Combine(shellOptions.Value.ShellsApplicationDataPath, "tenants.json");
        }

        public Task AddSourcesAsync(IConfigurationBuilder builder)
        {
            builder.AddJsonFile(_tenants, optional: true);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string tenant)
        {
            var settings = GetSettings();

            if (settings.GetValue(tenant) == null) return Task.CompletedTask;

            settings.Remove(tenant);
            File.WriteAllText(_tenants, settings.ToString());

            return Task.CompletedTask;
        }

        private JObject GetSettings() =>
            !File.Exists(_tenants) ? new JObject() : JObject.Parse(File.ReadAllText(_tenants));

        public async Task SaveAsync(string tenant, IDictionary<string, string> data)
        {
            JObject tenantsSettings;
            if (File.Exists(_tenants))
            {
                using (var file = File.OpenText(_tenants))
                {
                    using (var reader = new JsonTextReader(file))
                    {
                        tenantsSettings = await JObject.LoadAsync(reader);
                    }
                }
            }
            else
            {
                tenantsSettings = new JObject();
            }

            var settings = tenantsSettings.GetValue(tenant) as JObject ?? new JObject();

            foreach (var key in data.Keys)
            {
                if (data[key] != null)
                {
                    settings[key] = data[key];
                }
                else
                {
                    settings.Remove(key);
                }
            }

            tenantsSettings[tenant] = settings;

            using (var file = File.CreateText(_tenants))
            {
                using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
                {
                    await tenantsSettings.WriteToAsync(writer);
                }
            }
        }
    }
}
