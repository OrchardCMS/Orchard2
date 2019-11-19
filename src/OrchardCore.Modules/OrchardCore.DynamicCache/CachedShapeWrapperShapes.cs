using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Shapes;

namespace OrchardCore.DynamicCache
{
    public class CachedShapeWrapperShapes : IShapeAttributeProvider
    {
        [Shape]
        public IHtmlContent CachedShapeWrapper(IShape Shape, IServiceProvider ServiceProvider)
        {
            // No need to optimize this code as it will be used for debugging purpose

            var sb = new StringBuilder();
            var metadata = Shape.Metadata;
            var cache = metadata.Cache();

            sb.AppendLine($"<!-- CACHED SHAPE: {cache.CacheId} ({Guid.NewGuid()})");
            sb.AppendLine($"          VARY BY: {String.Join(", ", cache.Contexts)}");
            sb.AppendLine($"     DEPENDENCIES: {String.Join(", ", cache.Tags)}");
            sb.AppendLine($"       EXPIRES ON: {cache.ExpiresOn}");
            sb.AppendLine($"    EXPIRES AFTER: {cache.ExpiresAfter}");
            sb.AppendLine($"  EXPIRES SLIDING: {cache.ExpiresSliding}");
            sb.AppendLine("-->");

            using (var sw = new StringWriter())
            {
                var htmlEncoder = ServiceProvider.GetRequiredService<HtmlEncoder>();

                metadata.ChildContent.WriteTo(sw, htmlEncoder);
                sb.AppendLine(sw.ToString());
            }

            sb.AppendLine($"<!-- END CACHED SHAPE: {cache.CacheId} -->");

            return new HtmlString(sb.ToString());
        }
    }
}
