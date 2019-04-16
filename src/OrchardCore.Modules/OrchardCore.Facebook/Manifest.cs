using OrchardCore.Modules.Manifest;
using OrchardCore.Facebook;

[assembly: Module(
    Name = "Facebook",
    Author = "The Orchard Team",
    Website = "https://orchardproject.net",
    Version = "2.0.0",
    Category = "Facebook"
)]

[assembly: Feature(
    Id = FacebookConstants.Features.Core,
    Name = "Facebook",
    Category = "Facebook",
    Description = "Registers the core components used by the Facebook features."
)]

[assembly: Feature(
    Id = FacebookConstants.Features.Login,
    Name = "Facebook Login",
    Category = "Facebook",
    Description = "Auhenticates users from facebook.",
    Dependencies = new[] { FacebookConstants.Features.Core }
)]

[assembly: Feature(
    Id = FacebookConstants.Features.Widgets,
    Name = "Facebook Social Plugins Widgets",
    Category = "Facebook",
    Description = "Adds Social Plugins as Widgets.",
    Dependencies = new[] { FacebookConstants.Features.Core, "OrchardCore.Templates","OrchardCore.Widgets" }
)]