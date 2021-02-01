using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors.ShapePlacementStrategy;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Placements.Services;
using OrchardCore.Placements.ViewModels;
using OrchardCore.Routing;
using OrchardCore.Settings;

namespace OrchardCore.Placements.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly PlacementsManager _placementsManager;
        private readonly IHtmlLocalizer H;
        private readonly IStringLocalizer S;
        private readonly INotifier _notifier;
        private readonly ISiteService _siteService;
        private readonly dynamic New;

        public AdminController(
            ILogger<AdminController> logger,
            IAuthorizationService authorizationService,
            PlacementsManager placementsManager,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            IStringLocalizer<AdminController> stringLocalizer,
            INotifier notifier,
            ISiteService siteService,
            IShapeFactory shapeFactory)
        {
            _logger = logger;
            _authorizationService = authorizationService;
            _placementsManager = placementsManager;
            _notifier = notifier;
            _siteService = siteService;

            New = shapeFactory;
            H = htmlLocalizer;
            S = stringLocalizer;
        }

        public async Task<IActionResult> Index(ContentOptions options, PagerParameters pagerParameters)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManagePlacements))
            {
                return Forbid();
            }

            var siteSettings = await _siteService.GetSiteSettingsAsync();
            var pager = new Pager(pagerParameters, siteSettings.PageSize);

            var shapeTypes = await _placementsManager.ListShapePlacementsAsync();

            var shapeList = shapeTypes.Select(entry => new ShapePlacementViewModel
            {
                ShapeType = entry.Key
            }).ToList();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                shapeList = shapeList.Where(x => x.ShapeType.Contains(options.Search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var count = shapeList.Count();

            shapeList = shapeList.OrderBy(x => x.ShapeType)
                .Skip(pager.GetStartIndex())
                .Take(pager.PageSize).ToList();

            var pagerShape = (await New.Pager(pager)).TotalItemCount(count);

            var model = new ListShapePlacementsViewModel
            {
                ShapePlacements = shapeList,
                Pager = pagerShape,
                Options = options,
            };

            model.Options.ContentsBulkAction = new List<SelectListItem>() {
                new SelectListItem() { Text = S["Delete"], Value = nameof(ContentsBulkAction.Remove) }
            };

            return View("Index", model);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.Filter")]
        public ActionResult IndexFilterPOST(ListShapePlacementsViewModel model)
        {
            return RedirectToAction("Index", new RouteValueDictionary {
                { "Options.Search", model.Options.Search }
            });
        }

        public async Task<IActionResult> Create(string suggestion, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManagePlacements))
            {
                return Forbid();
            }

            var template = new PlacementNode[] { new PlacementNode() };

            var viewModel = new EditShapePlacementViewModel
            {
                Creating = true,
                ShapeType = suggestion,
                Nodes = JsonConvert.SerializeObject(template, Formatting.Indented)
            };

            return View("Edit", viewModel);
        }

        public async Task<IActionResult> Edit(string shapeType, string displayType = null, string contentType = null, string contentPart = null, string differentiator = null, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManagePlacements))
            {
                return Forbid();
            }

            var placementNodes = (await _placementsManager.GetShapePlacementsAsync(shapeType))?.ToList() ?? new List<PlacementNode>();

            if (!placementNodes.Any() || ShouldCreateNode(placementNodes, displayType, contentType, contentPart, differentiator))
            {
                var generatedNode = new PlacementNode
                {
                    DisplayType = displayType,
                    Differentiator = differentiator
                };
                if (!string.IsNullOrEmpty(contentType))
                {
                    generatedNode.Filters.Add("contentType", new JArray(contentType));
                }
                if (!string.IsNullOrEmpty(contentPart))
                {
                    generatedNode.Filters.Add("contentPart", new JArray(contentPart));
                }

                placementNodes.Add(generatedNode);
            }

            var viewModel = new EditShapePlacementViewModel
            {
                ShapeType = shapeType,
                Nodes = JsonConvert.SerializeObject(placementNodes, Formatting.Indented)
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("Edit")]
        public async Task<IActionResult> Edit(EditShapePlacementViewModel viewModel, string submit, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManagePlacements))
            {
                return Forbid();
            }

            if (viewModel.Creating && await _placementsManager.GetShapePlacementsAsync(viewModel.ShapeType) != null)
            {
                // Prevent overriding existing rules on creation
                _notifier.Warning(H["Placement rules for \"{0}\" already exists. Please edit existing rule.", viewModel.ShapeType]);
                return View(viewModel);
            }

            try
            {
                IEnumerable<PlacementNode> placementNodes = JsonConvert.DeserializeObject<PlacementNode[]>(viewModel.Nodes) ?? new PlacementNode[0];

                // Remove empty nodes
                placementNodes = placementNodes.Where(node => !IsEmpty(node));

                if (placementNodes.Any())
                {
                    // Save
                    await _placementsManager.UpdateShapePlacementsAsync(viewModel.ShapeType, placementNodes);
                    viewModel.Creating = false;

                    _notifier.Success(H["The \"{0}\" placement have been saved.", viewModel.ShapeType]);
                }
                else if (viewModel.Creating)
                {
                    _notifier.Warning(H["The \"{0}\" placement is empty.", viewModel.ShapeType]);
                    return View(viewModel);
                }
                else
                {
                    // Remove if empty
                    await _placementsManager.RemoveShapePlacementsAsync(viewModel.ShapeType);
                    _notifier.Success(H["The \"{0}\" placement has been deleted.", viewModel.ShapeType]);
                }
            }
            catch(JsonReaderException jsonException)
            {
                _notifier.Error(H["An error occurred while parsing the placement<br/>{0}", jsonException.Message]);
                return View(viewModel);
            }
            catch (Exception e)
            {
                _notifier.Error(H["An error occurred while saving the placement."]);
                _logger.LogError(e, "An error occurred while saving the placement.");
                return View(viewModel);
            }

            if (submit != "SaveAndContinue")
            {
                return RedirectToReturnUrlOrIndex(returnUrl);
            }

            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(string shapeType, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManagePlacements))
            {
                return Forbid();
            }

            await _placementsManager.RemoveShapePlacementsAsync(shapeType);
            _notifier.Success(H["The \"{0}\" placement has been deleted.", shapeType]);

            return RedirectToReturnUrlOrIndex(returnUrl);
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.BulkAction")]
        public async Task<ActionResult> IndexPost(ViewModels.ContentOptions options, IEnumerable<string> itemIds)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManagePlacements))
            {
                return Forbid();
            }

            if (itemIds?.Count() > 0)
            {
                switch (options.BulkAction)
                {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var item in itemIds)
                        {
                            await _placementsManager.RemoveShapePlacementsAsync(item);
                        }
                        _notifier.Success(H["Placements successfully removed."]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return RedirectToAction("Index");
        }

        private IActionResult RedirectToReturnUrlOrIndex(string returnUrl)
        {
            if ((String.IsNullOrEmpty(returnUrl) == false) && (Url.IsLocalUrl(returnUrl)))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        private static bool ShouldCreateNode(IEnumerable<PlacementNode> nodes, string displayType, string contentType, string contentPart, string differentiator)
        {
            if (string.IsNullOrEmpty(displayType) && string.IsNullOrEmpty(differentiator))
            {
                return false;
            }
            else
            {
                return !nodes.Any(node =>
                    (string.IsNullOrEmpty(displayType) || node.DisplayType == displayType) &&
                    (string.IsNullOrEmpty(contentType) || (node.Filters.ContainsKey("contentType") && FilterEquals(node.Filters["contentType"], contentType))) &&
                    (string.IsNullOrEmpty(contentPart) || (node.Filters.ContainsKey("contentPart") && FilterEquals(node.Filters["contentPart"], contentPart))) &&
                    (string.IsNullOrEmpty(differentiator) || node.Differentiator == differentiator));
            }
        }

        private static bool IsEmpty(PlacementNode node)
        {
            return string.IsNullOrEmpty(node.Location)
                && string.IsNullOrEmpty(node.ShapeType)
                && (node.Alternates == null || node.Alternates.Length == 0)
                && (node.Wrappers == null || node.Wrappers.Length == 0);
        }


        private static bool FilterEquals(JToken token, string value)
        {
            if (token is JArray)
            {
                var tokenValues = token.Values<string>();
                return tokenValues.Count() == 1 && tokenValues.First() == value;
            }
            else
            {
                return token.Value<string>() == value;
            }
        }
    }
}
