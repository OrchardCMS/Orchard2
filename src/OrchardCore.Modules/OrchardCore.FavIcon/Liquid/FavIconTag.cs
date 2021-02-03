using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Fluid.Tags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.FavIcon.Configuration;
using OrchardCore.Media;
using OrchardCore.Themes.Services;

namespace OrchardCore.FavIcon.Liquid
{
    public class FavIconTag : SimpleTag
    {
     
        public override async ValueTask<Completion> WriteToAsync(TextWriter writer, TextEncoder encoder, TemplateContext context)
        {
            if (!context.AmbientValues.TryGetValue("Services", out var servicesValue))
            {
                throw new ArgumentException("Services missing while invoking 'helper'");
            }

            var services = servicesValue as IServiceProvider;
           
            var settings = services.GetRequiredService<IOptions<FavIconSettings>>().Value;
            //var logger = services.GetRequiredService<ILogger>();
            var siteThemeService = services.GetRequiredService<ISiteThemeService>();
            var mediaFileStore = services.GetRequiredService<IMediaFileStore>();

            var themeId = (await siteThemeService.GetSiteThemeAsync()).Id;
            var mediaLibraryFolder = settings.MediaLibraryFolder;
            var basePath = string.Join('/', themeId, mediaFileStore.MapPathToPublicUrl(mediaLibraryFolder).Trim('/'));


            writer.WriteLine(@"    <link rel=""apple-touch-icon"" sizes=""180x180"" href=""/{0}/apple-touch-icon.png"">", basePath);
            writer.WriteLine(@"    <link rel=""icon"" type=""image/png"" sizes=""16x16"" href=""/{0}/favicon-16x16.png"">", basePath);
            writer.WriteLine(@"    <link rel=""icon"" type=""image/png"" sizes=""32x32"" href=""/{0}/favicon-32x32.png"">", basePath);
            writer.WriteLine(@"    <link rel=""manifest"" href=""/{0}/site.webmanifest"">", basePath);

            var tileColor = settings.TileColor;
            if (!string.IsNullOrWhiteSpace(tileColor))
            {
                writer.WriteLine(@"    <meta name=""msapplication-TileColor"" content=""{0}"">", tileColor);
               
            }

            var themeColor = settings.ThemeColor;
            if (!string.IsNullOrWhiteSpace(themeColor))
            {
                writer.WriteLine(@"    <meta name=""theme-color"" content=""{0}"">", themeColor);
               
            }

            return Completion.Normal;


        }
    }
}

