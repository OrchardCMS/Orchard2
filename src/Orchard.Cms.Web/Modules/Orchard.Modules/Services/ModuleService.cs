﻿using Microsoft.AspNetCore.Mvc.Localization;
using Orchard.DisplayManagement.Notify;
using OrchardCore.Extensions;
using OrchardCore.Extensions.Features;
using OrchardCore.Tenant;
using OrchardCore.Tenant.Descriptor;
using Orchard.Modules.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchard.Modules.Services
{
    public class ModuleService : IModuleService
    {
        private readonly IExtensionManager _extensionManager;
        private readonly ITenantDescriptorManager _tenantDescriptorManager;
        private readonly ITenantFeaturesManager _tenantFeaturesManager;
        private readonly INotifier _notifier;

        public ModuleService(
                IExtensionManager extensionManager,
                ITenantDescriptorManager tenantDescriptorManager,
                ITenantFeaturesManager tenantFeaturesManager,
                IHtmlLocalizer<AdminMenu> htmlLocalizer,
                INotifier notifier)
        {
            _notifier = notifier;
            _extensionManager = extensionManager;
            _tenantDescriptorManager = tenantDescriptorManager;
            _tenantFeaturesManager = tenantFeaturesManager;

            T = htmlLocalizer;
        }

        public IHtmlLocalizer T { get; set; }

        /// <summary>
        /// Retrieves an enumeration of the available features together with its state (enabled / disabled).
        /// </summary>
        /// <returns>An enumeration of the available features together with its state (enabled / disabled).</returns>
        public async Task<IEnumerable<ModuleFeature>> GetAvailableFeaturesAsync()
        {
            var enabledFeatures =
                await _tenantFeaturesManager.GetEnabledFeaturesAsync();

            var availableFeatures = _extensionManager.GetFeatures();

            return availableFeatures
                .Select(f => AssembleModuleFromDescriptor(f, enabledFeatures
                    .Any(sf => sf.Id == f.Id)));
        }

        /// <summary>
        /// Enables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be enabled.</param>
        public Task EnableFeaturesAsync(IEnumerable<string> featureIds)
        {
            return EnableFeaturesAsync(featureIds, false);
        }

        /// <summary>
        /// Enables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be enabled.</param>
        /// <param name="force">Boolean parameter indicating if the feature should enable it's dependencies if required or fail otherwise.</param>
        public async Task EnableFeaturesAsync(IEnumerable<string> featureIds, bool force)
        {
            var featuresToEnable = _extensionManager
                .GetFeatures()
                .Where(x => featureIds.Contains(x.Id));

            var enabledFeatures = await _tenantFeaturesManager.EnableFeaturesAsync(featuresToEnable, force);
            foreach (var enabledFeature in enabledFeatures)
            {
                _notifier.Success(T["{0} was enabled", enabledFeature.Name]);
            }
        }

        /// <summary>
        /// Disables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be disabled.</param>
        public Task DisableFeaturesAsync(IEnumerable<string> featureIds)
        {
            return DisableFeaturesAsync(featureIds, false);
        }

        /// <summary>
        /// Disables a list of features.
        /// </summary>
        /// <param name="featureIds">The IDs for the features to be disabled.</param>
        /// <param name="force">Boolean parameter indicating if the feature should disable the features which depend on it if required or fail otherwise.</param>
        public async Task DisableFeaturesAsync(IEnumerable<string> featureIds, bool force)
        {
            var featuresToDisable = _extensionManager
                .GetFeatures()
                .Where(x => featureIds.Contains(x.Id));

            var features = await _tenantFeaturesManager.DisableFeaturesAsync(featuresToDisable, force);
            foreach (var feature in features)
            {
                _notifier.Success(T["{0} was disabled", feature.Name]);
            }
        }

        ///// <summary>
        ///// Determines if a module was recently installed by using the project's last written time.
        ///// </summary>
        ///// <param name="extensionDescriptor">The extension descriptor.</param>
        //public bool IsRecentlyInstalled(ExtensionDescriptor extensionDescriptor) {
        //    DateTime lastWrittenUtc = _cacheManager.Get(extensionDescriptor, descriptor => {
        //        string projectFile = GetManifestPath(extensionDescriptor);
        //        if (!string.IsNullOrEmpty(projectFile)) {
        //            // If project file was modified less than 24 hours ago, the module was recently deployed
        //            return _virtualPathProvider.GetFileLastWriteTimeUtc(projectFile);
        //        }

        //        return DateTime.UtcNow;
        //    });

        //    return DateTime.UtcNow.Subtract(lastWrittenUtc) < new TimeSpan(1, 0, 0, 0);
        //}

        ///// <summary>
        ///// Retrieves the full path of the manifest file for a module's extension descriptor.
        ///// </summary>
        ///// <param name="extensionDescriptor">The module's extension descriptor.</param>
        ///// <returns>The full path to the module's manifest file.</returns>
        //private string GetManifestPath(ExtensionDescriptor extensionDescriptor) {
        //    string projectPath = _virtualPathProvider.Combine(extensionDescriptor.Location, extensionDescriptor.Id, "module.txt");

        //    if (!_virtualPathProvider.FileExists(projectPath)) {
        //        return null;
        //    }

        //    return projectPath;
        //}

        private static ModuleFeature AssembleModuleFromDescriptor(IFeatureInfo featureInfo, bool isEnabled)
        {
            return new ModuleFeature
            {
                Descriptor = featureInfo,
                IsEnabled = isEnabled
            };
        }

        //private void GenerateWarning(string messageFormat, string featureName, IEnumerable<string> featuresInQuestion) {
        //    if (featuresInQuestion.Count() < 1)
        //        return;

        //    Services.Notifier.Warning(T(
        //        messageFormat,
        //        featureName,
        //        featuresInQuestion.Count() > 1
        //            ? string.Join("",
        //                          featuresInQuestion.Select(
        //                              (fn, i) =>
        //                              T(i == featuresInQuestion.Count() - 1
        //                                    ? "{0}"
        //                                    : (i == featuresInQuestion.Count() - 2
        //                                           ? "{0} and "
        //                                           : "{0}, "), fn).ToString()).ToArray())
        //            : featuresInQuestion.First()));
        //}
    }
}