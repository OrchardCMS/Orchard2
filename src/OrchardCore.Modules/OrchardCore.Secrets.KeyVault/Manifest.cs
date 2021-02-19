using OrchardCore.Modules.Manifest;

[assembly: Module(
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Name = "Azure KeyVault Secrets Store",
    Description = "The Azure KeyVault Secrets module provides a KeyVault store for secrets.",
    Category = "Configuration",
    Dependencies = new[] { "OrchardCore.Secrets" }
)]
