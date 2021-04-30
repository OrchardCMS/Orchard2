using System.Collections.Generic;
using OrchardCore.Environment.Shell;

namespace OrchardCore.Setup.Core
{
    public interface IUrlService
    {
        string GetEncodedUrl(ShellSettings shellSettings, Dictionary<string, string> queryParams = null);
        string GetDisplayUrl(ShellSettings shellSettings);
    }
}
