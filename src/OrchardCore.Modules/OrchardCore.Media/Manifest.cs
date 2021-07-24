using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Media",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion
)]

[assembly: Feature(
    Id = "OrchardCore.Media",
    Name = "Media",
    Description = "The media module adds media management support.",
    Dependencies = new[] { "OrchardCore.ContentTypes" },
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "OrchardCore.Media.Cache",
    Name = "Media Cache",
    Description = "The media cache module adds remote file store cache support.",
    Dependencies = new[]
    {
        "OrchardCore.Media"
    },
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "OrchardCore.Media.Slugify",
    Name = "Media Slugify",
    Description = "The media slugify module slugifies new folders and files to make them SEO-friendly.",
    Dependencies = new[]
    {
        "OrchardCore.Media"
    },
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "OrchardCore.Media.Cache.BackgroundTask",
    Name = "Media Cache Background Task",
    Description = "Provides remote store cache and ImageSharp cache periodical purging ability.",
    Dependencies = new[]
    {
        "OrchardCore.Media.Cache"
    },
    Category = "Content Management"
)]
