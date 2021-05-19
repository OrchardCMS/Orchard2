using System;
using System.Threading.Tasks;
using OrchardCore.Data.Migration;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Events;
using OrchardCore.Environment.Shell.Models;
using YesSql;

namespace OrchardCore.ContentManagement
{
    public class MigrationsUninstallShellEventHandler : IShellEventHandler
    {
        private readonly IDataMigrationManager _dataMigrationManager;
        private readonly IShellFeaturesManager _shellFeaturesManager;
        private readonly ISession _session;

        public MigrationsUninstallShellEventHandler(IDataMigrationManager dataMigrationManager, IShellFeaturesManager shellFeaturesManager, ISession session)
        {
            _dataMigrationManager = dataMigrationManager;
            _shellFeaturesManager = shellFeaturesManager;
            _session = session;
        }

        public async Task Removing(ShellSettings shellSettings)
        {
            if (shellSettings.State == TenantState.Uninitialized) return;

            if (shellSettings.State == TenantState.Invalid)
                throw new InvalidOperationException($"Tenant reset action cannot be performed when tenant state is '{shellSettings.State}'.");

            foreach (var enabledFeature in await _shellFeaturesManager.GetEnabledFeaturesAsync())
            {
                await _dataMigrationManager.Uninstall(enabledFeature.Id);
            }
        }
    }
}
