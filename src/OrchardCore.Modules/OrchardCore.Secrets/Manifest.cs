using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Secrets",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion
)]


[assembly: Feature(
    Id = "OrchardCore.Secrets",
    Name = "Secrets",
    Description = "The secrets feature manages secrets that other modules can contribute to.",
    Category = "Configuration"
)]

[assembly: Feature(
    Id = "OrchardCore.Secrets.ConfigurationSecretStore",
    Name = "Configuration Secrets Store",
    Description = "The secrets configuration store is a readonly store for secrets.",
    Category = "Configuration",
    Dependencies = new[] { "OrchardCore.Secrets" }
)]
