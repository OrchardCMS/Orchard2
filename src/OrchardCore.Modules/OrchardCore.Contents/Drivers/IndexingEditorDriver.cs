using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.Contents.Models;
using OrchardCore.Contents.ViewModels;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Security;

namespace OrchardCore.Contents.Drivers
{
    public class IndexingEditorDriver : ContentPartDisplayDriver<IndexingPart>
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer S;

        public IndexingEditorDriver(IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<IndexingEditorDriver> stringLocalizer)
        {
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
            S = stringLocalizer;
        }

        public override async Task<IDisplayResult> EditAsync(IndexingPart part, BuildPartEditorContext context)
        {
            var currentUser = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(currentUser, StandardPermissions.SiteOwner, part))
            {
                return null;
            }

            var settings = context.TypePartDefinition.ContentTypeDefinition.GetSettings<FullTextAspectSettings>();

            if (settings.ExcludeIndexing)
            {
                return Initialize<IndexingEditorViewModel>("IndexingPart_Edit", model =>
                {
                    model.IsIndexed = part.IsIndexed;
                });
            }

            return null;
        }

        public override async Task<IDisplayResult> UpdateAsync(IndexingPart part, UpdatePartEditorContext context)
        {
            var currentUser = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(currentUser, StandardPermissions.SiteOwner, part))
            {
                return null;
            }

            var settings = context.TypePartDefinition.ContentTypeDefinition.GetSettings<FullTextAspectSettings>();

            if (settings.ExcludeIndexing)
            {
                var model = new IndexingEditorViewModel();
                await context.Updater.TryUpdateModelAsync(model, Prefix, m => m.IsIndexed);
                part.IsIndexed = model.IsIndexed;
            }

            return await EditAsync(part, context);
        }
    }
}
