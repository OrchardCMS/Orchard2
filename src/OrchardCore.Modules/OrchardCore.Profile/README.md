
# Profile (`OrchardCore.Profile`)

Provides user profiles  

### Usage

 - Once enabled, a profile view is provided {SITE_URL}/profile
 - A user must log in to visit the URL
 - If they are not logged in they will be redirected to the login screen 
 - If a user logs off on the profile screen they are redirected to the homepage
 - The Module is meant to be extended by other modules to provide value
 
 ## How Extend the Module in Your Module 
 ### `Startup`
 Required scopes to extend module

```
public override void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IProfileNavigationProvider, ProfileMenu>();            
    services.AddScoped<IDisplayManager<IProfile>, DisplayManager<IProfile>>();
    services.AddScoped<IDisplayDriver<IProfile>, DefaultProfileDisplayDriver>();
}
```
 ### `Profile Menu`
 Dynamically add menus in profile page 
```
using Microsoft.Extensions.Localization;
using OrchardCore.Profile.Navigation;
using System;
using System.Threading.Tasks;


namespace AdvancedForms
{
    public class ProfileMenu : IProfileNavigationProvider
    {
        public ProfileMenu(IStringLocalizer<ProfileMenu> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; set; }



        public Task BuildNavigation(string name, ProfileNavigationBuilder builder)
        {
            if (!String.Equals(name, "profile", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }
            builder
                .Add(T["Submitted Forms"], "1", advancedForms => advancedForms
                    .Url("/profile")
                    .AddClass("list-group-item list-group-item-action active"))
                .Add(T["Advanced Forms"], "2", advancedForms => advancedForms
                    .Url("/advancedForms")
                    .AddClass("list-group-item list-group-item-action"))
                .Add(T["Downloadable Forms"], "2", advancedForms => advancedForms
                    .Url("/downloadableForms")
                    .AddClass("list-group-item list-group-item-action"));
            return Task.CompletedTask;
        }
    }
}
```
 ### `Display Driver`
 Display view according to selected menu
 ```
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AdvancedForms.ViewModels;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.Profile;
using YesSql;

namespace AdvancedForms.Drivers
{
    public class DefaultProfileDisplayDriver : DisplayDriver<IProfile>
    {
        public const string GroupId = "general";
        private readonly INotifier _notifier;
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly ISession _session;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;

        public DefaultProfileDisplayDriver(
            INotifier notifier,
            IShellHost shellHost,
            ShellSettings shellSettings,
            ISession session,
            IContentItemDisplayManager contentItemDisplayManager,
            IHtmlLocalizer<DefaultProfileDisplayDriver> h)
        {
            _notifier = notifier;
            _shellHost = shellHost;
            _session = session;
            _shellSettings = shellSettings;
            _contentItemDisplayManager = contentItemDisplayManager;
            H = h;
        }

        IHtmlLocalizer H { get; set; }


        public async override Task<IDisplayResult> EditAsync(IProfile profile, IUpdateModel updater)
        {
            var query = _session.Query<ContentItem, ContentItemIndex>();
            var pageOfContentItems = await query.Where(o => o.ContentType == "AdvancedFormSubmissions" && o.Latest).OrderByDescending(o => o.CreatedUtc).ListAsync();
            if (profile.UserName.ToLower() != "admin")
            {
                pageOfContentItems = pageOfContentItems.Where(o => o.Owner == profile.UserName);
            }
            var contentItemSummaries = new List<dynamic>();
            foreach (var contentItem in pageOfContentItems)
            {
                contentItemSummaries.Add(await _contentItemDisplayManager.BuildDisplayAsync(contentItem, updater, "SubmissionProfile_ListItem"));
            }

            return await Task.FromResult<IDisplayResult>(
                    Initialize<ProfileViewModel>("List_Edit", item =>
                    {
                        item.ContentItemSummaries = contentItemSummaries;
                    }).Location("Content:1").OnGroup(GroupId)
            );
        }
    }
}
 ```
 ### `Display List`
 Layout of the view which is shown by display driver

 ### List.Edit
```
@model ProfileViewModel
<style>
    .btn-group{
        display : none;
    }
</style>

@if (Model.ContentItemSummaries.Count > 0)
{
    <ul class="list-group">
        @foreach (var contentItemSummary in Model.ContentItemSummaries)
        {
                @await DisplayAsync(contentItemSummary)
        }
    </ul>
}
else
{
    <div class="alert alert-info">
        @T["No result found."]
    </div>
}
```

 ### Content.SubmissionProfile.ListItem.cshtml
```
@using OrchardCore.ContentManagement
@inject IContentManager ContentManager
@{
    ContentItem contentItem = Model.ContentItem;
    var hasPublished = await ContentManager.HasPublishedVersionAsync(contentItem);
    var hasDraft = contentItem.HasDraft();
    string action = contentItem.Content.AutoroutePart.Path.ToString().Split("/")[1];
    string status = string.Empty;
    dynamic selectedContent = await ContentManager.GetAsync(contentItem.Content.AdvancedFormSubmissions.Status.Text.ToString(), VersionOptions.Published);
    if (selectedContent == null)
    {
        selectedContent = await ContentManager.GetAsync(contentItem.Content.AdvancedFormSubmissions.Status.Text.ToString(), VersionOptions.DraftRequired);
    }
    if (selectedContent != null)
    {
        status = selectedContent.DisplayText;
    }
}

<li class="list-group-item tedt">
    <div class="properties">
        <div class="related float-right">
            <a href="/AdvancedForms/@action/Edit/@contentItem.ContentItemId?returnUrl=@FullRequestPath" class="btn btn-primary btn-sm">@T["Edit"]</a>
            @if (hasPublished)
            {
                <a href="/@contentItem.Content.AutoroutePart.Path" class="btn btn-success btn-sm" itemprop="">@T["View"]</a>
            }
        </div>       
        <a href="/AdvancedForms/@action/Edit/@contentItem.ContentItemId">@action.Replace("-", " ")</a> 
        <br />
        <div class="metadata">
            @if (hasPublished)
            {
                <span class="badge badge-success" title="@T["Published"]">
                    <i class="fa fa-check" aria-hidden="true"></i> @T["Published"]
                </span>
            }
            @if (hasDraft)
            {
                <span class="badge badge-primary" title="@T["Draft"]">
                    <i class="fa fa-pencil" aria-hidden="true"></i> @T["Draft"]
                </span>
            }
                <span class="badge badge-secondary" title="@T[@status]" style="color:#fff;"> @T["Status - " + @status]</span>
            <div>
                <span class="hint">@T["Last modified {0} by {1}", (object)(await DisplayAsync(await New.TimeSpan(Utc: contentItem.ModifiedUtc))), contentItem.Author]</span>
            </div>
        </div>
    </div>
</li>
   
```

### ProfileViewModel.cs
```
using System.Collections.Generic;

namespace AdvancedForms.ViewModels
{
    public class ProfileViewModel
    {
        public List<dynamic> ContentItemSummaries { get; set; }
    }
}

```
