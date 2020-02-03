using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.Contents;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.Content.Controllers
{
    [Route("api/content")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
    public class ApiController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;

        public ApiController(
            IContentManager contentManager,
            IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
            _contentManager = contentManager;
        }

        [Route("{contentItemId}"), HttpGet]
        public async Task<IActionResult> Get(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewContent, contentItem))
            {
                return this.ChallengeOrForbid();
            }

            return Ok(contentItem);
        }

        [HttpDelete]
        [Route("{contentItemId}")]
        public async Task<IActionResult> Delete(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId);

            if (contentItem == null)
            {
                return StatusCode(204);
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.DeleteContent, contentItem))
            {
                return this.ChallengeOrForbid();
            }

            await _contentManager.RemoveAsync(contentItem);

            return Ok(contentItem);
        }

        [HttpPost]
        public async Task<IActionResult> Post(ContentItem newContentItem, bool draft = false)
        {
            var contentItem = await _contentManager.GetAsync(newContentItem.ContentItemId, VersionOptions.DraftRequired);

            if (contentItem == null)
            {
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.PublishContent))
                {
                    return this.ChallengeOrForbid();
                }

                await _contentManager.CreateAsync(newContentItem, VersionOptions.DraftRequired);

                contentItem = newContentItem;
            }
            else
            {
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContent, contentItem))
                {
                    return this.ChallengeOrForbid();
                }
            }

            if (contentItem != newContentItem)
            {
                contentItem.DisplayText = newContentItem.DisplayText;
                contentItem.ModifiedUtc = newContentItem.ModifiedUtc;
                contentItem.PublishedUtc = newContentItem.PublishedUtc;
                contentItem.CreatedUtc = newContentItem.CreatedUtc;
                contentItem.Owner = newContentItem.Owner;
                contentItem.Author = newContentItem.Author;

                contentItem.Apply(newContentItem);

                await _contentManager.UpdateAsync(contentItem);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!draft)
            {
                await _contentManager.PublishAsync(contentItem);
            }

            return Ok(contentItem);
        }
    }
}
