using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Elastic Search",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion
)]

[assembly: Feature(
    Id = "OrchardCore.Search.Elastic",
    Name = "ElasticSearch",
    Description = "Creates Elastic indexes to support search scenarios, introduces a preconfigured container-enabled content type.",
    Dependencies = new[]
    {
        "OrchardCore.Indexing",
        "OrchardCore.ContentTypes"
    },
    Category = "Content Management"
)]


[assembly: Feature(
    Id = "OrchardCore.Search.Elastic.ContentPicker",
    Name = "Elastic Content Picker",
    Description = "Provides a Elastic content picker field editor.",
    Dependencies = new[] { "OrchardCore.Search.Elastic", "OrchardCore.ContentFields" },
    Category = "Content Management"
)]
