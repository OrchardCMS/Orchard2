using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.Admin;
using OrchardCore.AdminTrees.Models;
using OrchardCore.AdminTrees.Services;
using OrchardCore.AdminTrees.ViewModels;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Settings;
using YesSql;

namespace OrchardCore.AdminTrees.Controllers
{
    [Admin]
    public class NodeController : Controller, IUpdateModel
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IDisplayManager<MenuItem> _displayManager;
        private readonly IEnumerable<IAdminNodeProviderFactory> _factories;
        private readonly IAdminTreeService _adminTreeService;
        private readonly INotifier _notifier;

        public NodeController(
            IAuthorizationService authorizationService,
            IDisplayManager<MenuItem> displayManager,
            IEnumerable<IAdminNodeProviderFactory> factories,
            IAdminTreeService adminTreeService,
            IShapeFactory shapeFactory,
            IStringLocalizer<NodeController> stringLocalizer,
            IHtmlLocalizer<NodeController> htmlLocalizer,
            INotifier notifier)
        {
            _displayManager = displayManager;
            _factories = factories;
            _adminTreeService = adminTreeService;
            _authorizationService = authorizationService;
            
            New = shapeFactory;
            _notifier = notifier;
            T = stringLocalizer;
            H = htmlLocalizer;
        }

        public dynamic New { get; set; }
        public IStringLocalizer T { get; set; }
        public IHtmlLocalizer H { get; set; }


        public async Task<IActionResult> List(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(id);

            if (tree == null)
            {
                return NotFound();
            }

            return View(await BuildDisplayViewModel(tree));
        }

        private async Task<AdminNodeListViewModel> BuildDisplayViewModel(AdminTree tree)
        {
            var thumbnails = new Dictionary<string, dynamic>();
            foreach (var factory in _factories)
            {
                var treeNode = factory.Create();
                dynamic thumbnail = await _displayManager.BuildDisplayAsync(treeNode, this, "TreeThumbnail");
                thumbnail.TreeNode = treeNode;
                thumbnails.Add(factory.Name, thumbnail);
            }

            var model = new AdminNodeListViewModel
            {
                AdminTree = tree,
                Thumbnails = thumbnails,
            };

            return model;
        }

        public async Task<IActionResult> Create(string id, string type)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(id);

            if (tree == null)
            {
                return NotFound();
            }

            var treeNode = _factories.FirstOrDefault(x => x.Name == type)?.Create();

            if (treeNode == null)
            {
                return NotFound();
            }

            var model = new AdminNodeEditViewModel
            {
                AdminTreeId = id,
                AdminNode = treeNode,
                AdminNodeId = treeNode.UniqueId,
                AdminNodeType = type,
                Editor = await _displayManager.BuildEditorAsync(treeNode, updater: this, isNew: true)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AdminNodeEditViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(model.AdminTreeId);

            if (tree == null)
            {
                return NotFound();
            }

            var treeNode = _factories.FirstOrDefault(x => x.Name == model.AdminNodeType)?.Create();

            if (treeNode == null)
            {
                return NotFound();
            }

            dynamic editor = await _displayManager.UpdateEditorAsync(treeNode, updater: this, isNew: true);
            editor.TreeNode = treeNode;

            if (ModelState.IsValid)
            {
                treeNode.UniqueId = model.AdminNodeId;
                tree.MenuItems.Add(treeNode);
                await _adminTreeService.SaveAsync(tree);

                _notifier.Success(H["Admin node added successfully"]);
                return RedirectToAction("List", new { id = model.AdminTreeId });
            }

            model.Editor = editor;

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> Edit(string id, string treeNodeId)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(id);

            if (tree == null)
            {
                return NotFound();
            }

            var treeNode = tree.GetMenuItemById(treeNodeId);

            if (treeNode == null)
            {
                return NotFound();
            }

            var model = new AdminNodeEditViewModel
            {
                AdminTreeId = id,
                AdminNode = treeNode,
                AdminNodeId = treeNode.UniqueId,
                AdminNodeType = treeNode.GetType().Name,
                Editor = await _displayManager.BuildEditorAsync(treeNode, updater: this, isNew: false)
            };

            model.Editor.TreeNode = treeNode;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AdminNodeEditViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(model.AdminTreeId);

            if (tree == null)
            {
                return NotFound();
            }

            var treeNode = tree.GetMenuItemById(model.AdminNodeId);

            if (treeNode == null)
            {
                return NotFound();
            }

            var editor = await _displayManager.UpdateEditorAsync(treeNode, updater: this, isNew: false);

            if (ModelState.IsValid)
            {
                await _adminTreeService.SaveAsync(tree);

                _notifier.Success(H["Admin node updated successfully"]);
                return RedirectToAction(nameof(List), new { id = model.AdminTreeId});
            }

            _notifier.Error(H["The admin node has validation errors"]);
            model.Editor = editor;

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, string treeNodeId)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(id);

            if (tree == null)
            {
                return NotFound();
            }

            var treeNode = tree.GetMenuItemById(treeNodeId);

            if (treeNode == null)
            {
                return NotFound();
            }

            if (tree.RemoveMenuItem(treeNode) == false)
            {
                return new StatusCodeResult(500);
            }

            await _adminTreeService.SaveAsync(tree);

            _notifier.Success(H["Admin node deleted successfully"]);

            return RedirectToAction(nameof(List), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(string id, string treeNodeId)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(id);

            if (tree == null)
            {
                return NotFound();
            }

            var treeNode = tree.GetMenuItemById(treeNodeId);

            if (treeNode == null)
            {
                return NotFound();
            }

            treeNode.Enabled = !treeNode.Enabled;

            await _adminTreeService.SaveAsync(tree);

            _notifier.Success(H["Admin node toggled successfully"]);

            return RedirectToAction(nameof(List), new { id = id});
        }


        [HttpPost]
        public async Task<IActionResult> MoveNode(string treeId, string nodeToMoveId,
            string destinationNodeId, int position)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageAdminTree))
            {
                return Unauthorized();
            }

            var tree = await _adminTreeService.GetByIdAsync(treeId);

            if ((tree == null) || (tree.MenuItems == null))
            {
                return NotFound();
            }


            var nodeToMove = tree.GetMenuItemById(nodeToMoveId);
            if (nodeToMove == null)
            {
                return NotFound();
            }

            var destinationNode = tree.GetMenuItemById(destinationNodeId); // don't check for null. When null the item will be moved to the root.

            if (tree.RemoveMenuItem(nodeToMove) == false)
            {
                return StatusCode(500);
            }

            if (tree.InsertMenuItemAt(nodeToMove, destinationNode, position) == false)
            {
                return StatusCode(500);
            }

            await _adminTreeService.SaveAsync(tree);

            return Ok();
        }
    }
}

