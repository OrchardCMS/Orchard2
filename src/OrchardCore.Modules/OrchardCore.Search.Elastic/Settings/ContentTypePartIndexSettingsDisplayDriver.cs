using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Indexing;

namespace OrchardCore.Search.Elastic.Settings
{
    public class ContentTypePartIndexSettingsDisplayDriver : ContentTypePartDefinitionDisplayDriver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public ContentTypePartIndexSettingsDisplayDriver(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(ContentTypePartDefinition contentTypePartDefinition, IUpdateModel updater)
        {
            if (!await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, Permissions.ManageIndexes))
            {
                return null;
            }

            return Initialize<ElasticContentIndexSettingsViewModel>("ElasticContentIndexSettings_Edit", model =>
            {
                model.ContentIndexSettings = contentTypePartDefinition.GetSettings<ContentIndexSettings>();
            }).Location("Content:10");
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentTypePartDefinition contentTypePartDefinition, UpdateTypePartEditorContext context)
        {
            if (!await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, Permissions.ManageIndexes))
            {
                return null;
            }

            var model = new ElasticContentIndexSettingsViewModel();

            await context.Updater.TryUpdateModelAsync(model, Prefix);

            context.Builder.WithSettings(model.ContentIndexSettings);

            return await EditAsync(contentTypePartDefinition, context.Updater);
        }
    }
}
