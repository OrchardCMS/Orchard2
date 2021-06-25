using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Data Localization",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "Provides support for data localization.",
    Category = "Internationalization",
    Dependencies = new[] { "OrchardCore.Localization" }
)]
