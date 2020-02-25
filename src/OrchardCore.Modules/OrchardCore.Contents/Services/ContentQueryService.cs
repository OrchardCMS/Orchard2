using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Contents.ViewModels;
using YesSql;
using YesSql.Services;

namespace OrchardCore.Contents.Services
{
    public class ContentQueryService : IContentQueryService
    {
        private readonly YesSql.ISession _session;
        private readonly IAuthorizationService _authorizationService;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ContentQueryService(
            YesSql.ISession session,
            IAuthorizationService authorizationService,
            IContentDefinitionManager contentDefinitionManager,
            IHttpContextAccessor httpContextAccessor

            )
        {
            _session = session;
            _authorizationService = authorizationService;
            _contentDefinitionManager = contentDefinitionManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IQuery<ContentItem, ContentItemIndex>> GetQueryByOptions(OrchardCore.Contents.ViewModels.ContentOptions options)
        {
            ClaimsPrincipal user = null;
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                user = httpContext.User;
            }

            var query = _session.Query<ContentItem, ContentItemIndex>();

            if (!string.IsNullOrEmpty(options.DisplayText))
            {
                query = query.With<ContentItemIndex>(x => x.DisplayText.Contains(options.DisplayText));
            }

            switch (options.ContentsStatus)
            {
                case ContentsStatus.Published:
                    query = query.With<ContentItemIndex>(x => x.Published);
                    break;
                case ContentsStatus.Draft:
                    query = query.With<ContentItemIndex>(x => x.Latest && !x.Published);
                    break;
                case ContentsStatus.AllVersions:
                    query = query.With<ContentItemIndex>(x => x.Latest);
                    break;
                default:
                    query = query.With<ContentItemIndex>(x => x.Latest);
                    break;
            }

            if (options.ContentsStatus == ContentsStatus.Owner)
            {

                if (user != null)
                {
                    query = query.With<ContentItemIndex>(x => x.Owner == user.Identity.Name);
                }

            }
            if (!string.IsNullOrEmpty(options.SelectedContentType))
            {
                // We display a specific type even if it's not listable so that admin pages
                // can reuse the Content list page for specific types.
                query = query.With<ContentItemIndex>(x => x.ContentType == options.SelectedContentType);


            }
            else
            {
                var listableTypes = (await GetListableTypesAsync(user)).Select(t => t.Name).ToArray();
                if (listableTypes.Any())
                {
                    query = query.With<ContentItemIndex>(x => x.ContentType.IsIn(listableTypes));
                }
            }

            switch (options.OrderBy)
            {
                case ContentsOrder.Modified:
                    query = query.OrderByDescending(x => x.ModifiedUtc);
                    break;
                case ContentsOrder.Published:
                    query = query.OrderByDescending(cr => cr.PublishedUtc);
                    break;
                case ContentsOrder.Created:
                    query = query.OrderByDescending(cr => cr.CreatedUtc);
                    break;
                case ContentsOrder.Title:
                    query = query.OrderBy(cr => cr.DisplayText);
                    break;
                default:
                    query = query.OrderByDescending(cr => cr.ModifiedUtc);
                    break;
            }
            return query;
        }

        public async Task<IEnumerable<ContentTypeDefinition>> GetListableTypesAsync(ClaimsPrincipal user)
        {
            var listable = new List<ContentTypeDefinition>();
            foreach (var ctd in _contentDefinitionManager.ListTypeDefinitions())
            {
                if (ctd.GetSettings<ContentTypeSettings>().Listable)
                {
                    if (user != null)
                    {
                        var authorized = await _authorizationService.AuthorizeAsync(user, Permissions.EditContent, await _contentManager.NewAsync(ctd.Name));
                        if (authorized)
                        {
                            listable.Add(ctd);
                        }
                    }

                }
            }
            return listable;
        }

    }
}
