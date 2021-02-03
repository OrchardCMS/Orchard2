using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "FavIcon",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion)]

[assembly: Feature(
    Id = "OrchardCore.FavIcon",
    Name = "FavIcon",
    Category = "Design",
    Description = "Provides FavIcon functionality.")]
