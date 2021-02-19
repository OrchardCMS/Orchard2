using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Deployment",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Category = "Deployment",
    Dependencies = new string[] { "OrchardCore.Secrets" }
)]
