using System;
using System.Collections.Generic;
using System.Linq;

namespace OrchardCore.Modules.Manifest
{
    /// <summary>
    /// Defines a Module which is composed of features.
    /// If the Module has only one default feature, it may be defined there.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModuleAttribute : FeatureAttribute
    {
        public ModuleAttribute()
        {
        }

        public virtual string Type => "Module";
        public new bool Exists => Id != null;

        /// <Summary>
        /// This identifier is overridden at runtime by the assembly name
        /// </Summary>
        public new string Id { get; internal set; }

        /// <Summary>The name of the developer.</Summary>
        public string Author { get; set; } = String.Empty;

        /// <Summary>The URL for the website of the developer.</Summary>
        public string Website { get; set; } = String.Empty;

        /// <Summary>The version number in SemVer format.</Summary>
        public string Version { get; set; } = "0.0";

        /// <Summary>A comma-separated lists of tags.</Summary>
        public string Tags { get; set; } = String.Empty;

        public List<FeatureAttribute> Features { get; } = new List<FeatureAttribute>();
    }

    /// <summary>
    /// Defines a Feature in a Module, can be used multiple times.
    /// If at least one Feature is defined, the Module default feature is ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class FeatureAttribute : Attribute
    {
        public FeatureAttribute()
        {
        }

        public bool Exists => Id != null;

        /// <Summary>The identifier of the feature.</Summary>
        public string Id { get; set; }

        /// <Summary>
        /// Human-readable name of the feature. If not provided, the identifier will be used.
        /// </Summary>
        public string Name { get; set; }

        /// <Summary>A brief summary of what the feature does.</Summary>
        public string Description { get; set; } = String.Empty;

        /// <Summary>
        /// A comma-separated list of features that the feature depends on.
        /// So that its drivers / handlers are invoked after those of dependencies.
        /// </Summary>
        public string Dependencies { get; set; } = String.Empty;

        /// <Summary>
        /// The priority of the feature without breaking the dependencies order.
        /// higher is the priority, later the drivers / handlers are invoked.
        /// </Summary>
        public string Priority { get; set; } = "0";

        /// <Summary>
        /// The group (by category) that the feature belongs.
        /// If not provided, defaults to 'Uncategorized'.
        /// </Summary>
        public string Category { get; set; }
    }

    /// <summary>
    /// Marks an assembly as a module, auto generated on building.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModuleMarkerAttribute : Attribute
    {
        public ModuleMarkerAttribute()
        {
        }
    }

    /// <summary>
    /// Names of modules referenced by the application, auto generated on building.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModuleNamesAttribute : Attribute
    {
        public ModuleNamesAttribute(string names)
        {
            Names = names ?? String.Empty;
        }

        /// <Summary>
        /// A semicolon-separated list of module names.
        /// </Summary>
        public string Names { get; }
    }

    /// <summary>
    /// Maps module assets to their project location while in debug mode, auto generated on building.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModuleAssetsMapAttribute : Attribute
    {
        public ModuleAssetsMapAttribute(string assets)
        {
            Assets = assets ?? String.Empty;
        }

        /// <Summary>
        /// A semicolon-separated list of module assets in the form of '{ModuleAssetPath}|{ProjectAssetPath}'.
        /// </Summary>
        public string Assets { get; }
    }
}