using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using OrchardCore.Sitemaps.Models;
using OrchardCore.Sitemaps.Services;
using OrchardCore.Sitemaps.ViewModels;

namespace OrchardCore.Sitemaps.Drivers
{
    public class CustomPathSitemapSourceDriver : DisplayDriver<SitemapSource, CustomPathSitemapSource>
    {
        private readonly IStringLocalizer S;

        public CustomPathSitemapSourceDriver(
            IStringLocalizer<CustomPathSitemapSourceDriver> localizer
        )
        {
            S = localizer;
        }
        
        public override IDisplayResult Display(CustomPathSitemapSource sitemapSource)
        {
            return Combine(
                View("CustomPathSitemapSource_SummaryAdmin", sitemapSource).Location("SummaryAdmin", "Content"),
                View("CustomPathSitemapSource_Thumbnail", sitemapSource).Location("Thumbnail", "Content")
            );
        }

        public override IDisplayResult Edit(CustomPathSitemapSource sitemapSource, IUpdateModel updater)
        {
            return Initialize<CustomPathSitemapSourceViewModel>("CustomPathSitemapSource_Edit", model =>
            {
                model.Url = sitemapSource.Url;
                model.Priority = sitemapSource.Priority;
                model.ChangeFrequency = sitemapSource.ChangeFrequency;
                
            }).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(CustomPathSitemapSource sitemap, UpdateEditorContext context)
        {
            var model = new CustomPathSitemapSourceViewModel();

            if (await context.Updater.TryUpdateModelAsync(model,
                    Prefix,
                    m => m.Url,
                    m => m.Priority,
                    m => m.ChangeFrequency
                ))
            {
                sitemap.Url = model.Url;
                sitemap.Priority = model.Priority;
                sitemap.ChangeFrequency = model.ChangeFrequency;     
                sitemap.LastUpdate = DateTime.Now;

                if (sitemap.Url?.IndexOfAny(CustomPathSitemapSource.InvalidCharactersForPath) > -1 || sitemap.Url?.IndexOf(' ') > -1 || sitemap.Url?.IndexOf("//") > -1)
                {
                    var invalidCharactersForMessage = string.Join(", ", CustomPathSitemapSource.InvalidCharactersForPath.Select(c => $"\"{c}\""));
                    context.Updater.ModelState.AddModelError(Prefix, sitemap.Url, S["Please do not use any of the following characters in your permalink: {0}. No spaces, or consecutive slashes, are allowed (please use dashes or underscores instead).", invalidCharactersForMessage]);
                }

                if (sitemap.Url?.Length > CustomPathSitemapSource.MaxPathLength)
                {
                    context.Updater.ModelState.AddModelError(Prefix, sitemap.Url, S["Your path is too long. The path can only be up to {0} characters.", CustomPathSitemapSource.MaxPathLength]);
                }
            };

            return Edit(sitemap, context.Updater);
        }
    }
}
