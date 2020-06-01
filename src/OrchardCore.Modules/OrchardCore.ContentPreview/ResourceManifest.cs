using OrchardCore.ResourceManagement;

namespace OrchardCore.ContentPreview
{
    public class ResourceManifest : IResourceManifestProvider
    {
        public void BuildManifests(IResourceManifestBuilder builder)
        {
            var manifest = builder.Add();

            manifest
                .DefineScript("oc-contentpreview-edit")
                .SetUrl("~/OrchardCore.ContentPreview/Scripts/contentpreview.edit.min.js", "~/OrchardCore.ContentPreview/Scripts/contentpreview.edit.js")
                .SetVersion("1.0");
        }
    }
}
