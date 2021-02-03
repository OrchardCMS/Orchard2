using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.FavIcon.Configuration;
using OrchardCore.Themes.Services;
using OrchardCore.Media;

namespace OrchardCore.FavIcon.TagHelpers
{
    [HtmlTargetElement("head", TagStructure = TagStructure.Unspecified)]
    public class FavIconTagHelper : TagHelper
    {
        private readonly ISiteThemeService siteThemeService;
        private readonly IMediaFileStore mediaFileStore;
        private readonly FavIconSettings settings;
        private readonly ILogger logger;

        public FavIconTagHelper(ISiteThemeService siteThemeService, IMediaFileStore mediaFileStore, IOptions<FavIconSettings> optionsAccessor, ILogger<FavIconTagHelper> logger)
        {
            this.settings = optionsAccessor.Value;
            this.logger = logger;
            this.siteThemeService = siteThemeService;
            this.mediaFileStore = mediaFileStore;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var contentTask = output.GetChildContentAsync();

            var themeId = (await siteThemeService.GetSiteThemeAsync()).Id;
            var mediaLibraryFolder = settings.MediaLibraryFolder;
            var basePath = string.Join('/', themeId, mediaFileStore.MapPathToPublicUrl(mediaLibraryFolder)).TrimEnd('/');

            var content = await contentTask;
            content.AppendFormat(@"    <link rel=""apple-touch-icon"" sizes=""180x180"" href=""/{0}/apple-touch-icon.png"">", basePath);
            content.AppendLine();
            content.AppendFormat(@"    <link rel=""icon"" type=""image/png"" sizes=""16x16"" href=""/{0}/favicon-16x16.png"">", basePath);
            content.AppendLine();
            content.AppendFormat(@"    <link rel=""icon"" type=""image/png"" sizes=""32x32"" href=""/{0}/favicon-32x32.png"">", basePath);
            content.AppendLine();
            content.AppendFormat(@"    <link rel=""manifest"" href=""/{0}/site.webmanifest"">", basePath);
            content.AppendLine();

            var tileColor = settings.TileColor;
            if (!string.IsNullOrWhiteSpace(tileColor))
            {
                content.AppendFormat(@"    <meta name=""msapplication-TileColor"" content=""{0}"">", tileColor);
                content.AppendLine();
            }

            var themeColor = settings.ThemeColor;
            if (!string.IsNullOrWhiteSpace(themeColor))
            {
                content.AppendFormat(@"    <meta name=""theme-color"" content=""{0}"">", themeColor);
                content.AppendLine();
            }

            output.Content = content;
        }
    }
}
