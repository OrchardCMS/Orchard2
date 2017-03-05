﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Orchard.Environment.Extensions.Features;
using Orchard.Environment.Extensions.Loaders;

namespace Orchard.Environment.Extensions
{
    public interface IExtensionManager
    {
        IExtensionInfo GetExtension(string extensionId);
        IEnumerable<IExtensionInfo> GetExtensions();
        Task<ExtensionEntry> LoadExtensionAsync(IExtensionInfo extensionInfo);

        IEnumerable<IFeatureInfo> GetFeatures();
        IEnumerable<IFeatureInfo> GetFeatures(string[] featureIdsToLoad);
        IEnumerable<IFeatureInfo> GetFeatureDependencies(string featureId);
        IEnumerable<IFeatureInfo> GetDependentFeatures(string featureId);
        Task<IEnumerable<FeatureEntry>> LoadFeaturesAsync();
        Task<IEnumerable<FeatureEntry>> LoadFeaturesAsync(string[] featureIdsToLoad);
    }

    public static class IExtensionManagerExtensions
    {
        public static IEnumerable<FeatureEntry> GetFeatureEntries(this IExtensionManager manager)
        {
            return manager.LoadFeaturesAsync().GetAwaiter().GetResult();
        }
    }
}
