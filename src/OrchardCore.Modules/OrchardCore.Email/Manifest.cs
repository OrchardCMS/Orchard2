using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Email",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "Provides email settings configuration and a default email service based on SMTP.",
    Dependencies = new[] { "OrchardCore.Resources", "OrchardCore.Secrets" },
    Category = "Messaging"
)]
