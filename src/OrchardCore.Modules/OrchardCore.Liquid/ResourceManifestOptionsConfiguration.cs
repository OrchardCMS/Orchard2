using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;

namespace OrchardCore.Liquid
{
    public class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
    {
        private static ResourceManifest _manifest;

        static ResourceManagementOptionsConfiguration()
        {
            _manifest = new ResourceManifest();

            _manifest
                .DefineScript("monaco-liquid-intellisense")
                .SetUrl("~/OrchardCore.Liquid/monaco/liquid-intellisense.js")
                .SetDependencies("monaco")
                .SetVersion("1.0.0");

            _manifest
                .DefineScript("liquid-intellisense")
                .SetDependencies("monaco-liquid-intellisense")
                .SetUrl("~/OrchardCore.Liquid/Scripts/liquid-intellisense.js");
        }

        public void Configure(ResourceManagementOptions options)
        {
            options.ResourceManifests.Add(_manifest);
        }
    }
}
