using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.ContentTypes.Services;
using OrchardCore.ContentTypes.ViewModels;
using OrchardCore.Data.Documents;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Routing;

namespace OrchardCore.ContentTypes.Controllers
{
    public class AdminController : Controller
    {
        private readonly IContentDefinitionService _contentDefinitionService;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IDocumentStore _documentStore;
        private readonly IContentDefinitionDisplayManager _contentDefinitionDisplayManager;
        private readonly IHtmlLocalizer H;
        private readonly IStringLocalizer S;
        private readonly INotifier _notifier;
        private readonly IUpdateModelAccessor _updateModelAccessor;

        public AdminController(
            IContentDefinitionDisplayManager contentDefinitionDisplayManager,
            IContentDefinitionService contentDefinitionService,
            IContentDefinitionManager contentDefinitionManager,
            IAuthorizationService authorizationService,
            IDocumentStore documentStore,
            IHtmlLocalizer<AdminController> htmlLocalizer,
            IStringLocalizer<AdminController> stringLocalizer,
            INotifier notifier,
            IUpdateModelAccessor updateModelAccessor)
        {
            _notifier = notifier;
            _contentDefinitionDisplayManager = contentDefinitionDisplayManager;
            _documentStore = documentStore;
            _authorizationService = authorizationService;
            _contentDefinitionService = contentDefinitionService;
            _contentDefinitionManager = contentDefinitionManager;
            _updateModelAccessor = updateModelAccessor;

            H = htmlLocalizer;
            S = stringLocalizer;
        }

        public Task<ActionResult> Index()
        {
            return List();
        }

        #region Types

        public async Task<ActionResult> List()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewContentTypes))
            {
                return Forbid();
            }

            return View("List", new ListContentTypesViewModel
            {
                Types = _contentDefinitionService.GetTypes()
            });
        }

        public async Task<ActionResult> Create(string suggestion)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            return View(new CreateTypeViewModel { DisplayName = suggestion, Name = suggestion.ToSafeName() });
        }

        [HttpPost, ActionName("Create")]
        public async Task<ActionResult> CreatePOST(CreateTypeViewModel viewModel, string admin = null, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            viewModel.DisplayName = viewModel.DisplayName?.Trim() ?? String.Empty;
            viewModel.Name = viewModel.Name ?? String.Empty;

            if (String.IsNullOrWhiteSpace(viewModel.DisplayName))
            {
                ModelState.AddModelError("DisplayName", S["The Display Name can't be empty."]);
            }

            if (_contentDefinitionService.LoadTypes().Any(t => String.Equals(t.DisplayName.Trim(), viewModel.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("DisplayName", S["A type with the same Display Name already exists."]);
            }

            if (String.IsNullOrWhiteSpace(viewModel.Name))
            {
                ModelState.AddModelError("Name", S["The Technical Name can't be empty."]);
            }

            if (!String.IsNullOrWhiteSpace(viewModel.Name) && !viewModel.Name[0].IsLetter())
            {
                ModelState.AddModelError("Name", S["The Technical Name must start with a letter."]);
            }

            if (!String.Equals(viewModel.Name, viewModel.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Name", S["The Technical Name contains invalid characters."]);
            }

            if (_contentDefinitionService.LoadTypes().Any(t => String.Equals(t.Name.Trim(), viewModel.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Name", S["A type with the same Technical Name already exists."]);
            }

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();
                return View(viewModel);
            }

            var contentTypeDefinition = _contentDefinitionService.AddType(viewModel.Name, viewModel.DisplayName);

            var typeViewModel = new EditTypeViewModel(contentTypeDefinition);

            _notifier.Success(H["The \"{0}\" content type has been created.", typeViewModel.DisplayName]);

            return RedirectToAction("AddPartsTo", new { id = typeViewModel.Name, admin = admin, returnUrl = returnUrl});
        }

        public async Task<ActionResult> Edit(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.GetType(id);

            if (typeViewModel == null)
            {
                return NotFound();
            }

            typeViewModel.Editor = await _contentDefinitionDisplayManager.BuildTypeEditorAsync(typeViewModel.TypeDefinition, _updateModelAccessor.ModelUpdater);

            return View(typeViewModel);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("submit.Save")]
        public async Task<ActionResult> EditPOST(string id, EditTypeViewModel viewModel)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var contentTypeDefinition = _contentDefinitionManager.LoadTypeDefinition(id);

            if (contentTypeDefinition == null)
            {
                return NotFound();
            }

            viewModel.Settings = contentTypeDefinition.Settings;
            viewModel.TypeDefinition = contentTypeDefinition;
            viewModel.DisplayName = contentTypeDefinition.DisplayName;
            viewModel.Editor = await _contentDefinitionDisplayManager.UpdateTypeEditorAsync(contentTypeDefinition, _updateModelAccessor.ModelUpdater);

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();

                return View(viewModel);
            }
            else
            {
                var ownedPartDefinition = _contentDefinitionManager.LoadPartDefinition(contentTypeDefinition.Name);
                if (ownedPartDefinition != null && viewModel.OrderedFieldNames != null)
                {
                    _contentDefinitionService.AlterPartFieldsOrder(ownedPartDefinition, viewModel.OrderedFieldNames);
                }
                _contentDefinitionService.AlterTypePartsOrder(contentTypeDefinition, viewModel.OrderedPartNames);
                _notifier.Success(H["\"{0}\" settings have been saved.", contentTypeDefinition.Name]);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("submit.Delete")]
        public async Task<IActionResult> Delete(string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.LoadType(id);

            if (typeViewModel == null)
            {
                return NotFound();
            }

            _contentDefinitionService.RemoveType(id, true);

            _notifier.Success(H["\"{0}\" has been removed.", typeViewModel.DisplayName]);

            return RedirectToReturnUrlOrAction(returnUrl, nameof(List), null);
        }

        public async Task<ActionResult> AddPartsTo(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.GetType(id);

            if (typeViewModel == null)
            {
                return NotFound();
            }

            var typePartNames = new HashSet<string>(typeViewModel.TypeDefinition.Parts.Select(p => p.PartDefinition.Name));

            var viewModel = new AddPartsViewModel
            {
                Type = typeViewModel,
                PartSelections = _contentDefinitionService.GetParts(metadataPartsOnly: false)
                    .Where(cpd => !typePartNames.Contains(cpd.Name) && cpd.PartDefinition != null ? cpd.PartDefinition.GetSettings<ContentPartSettings>().Attachable : false)
                    .Select(cpd => new PartSelectionViewModel { PartName = cpd.Name, PartDisplayName = cpd.DisplayName, PartDescription = cpd.Description })
                    .ToList()
            };

            return View(viewModel);
        }

        public async Task<ActionResult> AddReusablePartTo(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.GetType(id);

            if (typeViewModel == null)
            {
                return NotFound();
            }

            var reusableParts = _contentDefinitionService.GetParts(metadataPartsOnly: false)
                    .Where(cpd => cpd.PartDefinition != null ?
                        (cpd.PartDefinition.GetSettings<ContentPartSettings>().Attachable &&
                        cpd.PartDefinition.GetSettings<ContentPartSettings>().Reusable) : false);

            var viewModel = new AddReusablePartViewModel
            {
                Type = typeViewModel,
                PartSelections = reusableParts
                    .Select(cpd => new PartSelectionViewModel { PartName = cpd.Name, PartDisplayName = cpd.DisplayName, PartDescription = cpd.Description })
                    .ToList(),
                SelectedPartName = reusableParts.FirstOrDefault()?.Name
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("AddPartsTo")]
        public async Task<ActionResult> AddPartsToPOST(string id, string admin = null, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.LoadType(id);

            if (typeViewModel == null)
            {
                return NotFound();
            }

            var viewModel = new AddPartsViewModel();
            if (!await TryUpdateModelAsync(viewModel))
            {
                return await AddPartsTo(id);
            }

            var partsToAdd = viewModel.PartSelections.Where(ps => ps.IsSelected).Select(ps => ps.PartName);
            foreach (var partToAdd in partsToAdd)
            {
                _contentDefinitionService.AddPartToType(partToAdd, typeViewModel.Name);
                _notifier.Success(H["The \"{0}\" part has been added.", partToAdd]);
            }

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();
                return await AddPartsTo(id);
            }

            return RedirectToAction("Edit", new { id, admin, returnUrl });
        }

        [HttpPost, ActionName("AddReusablePartTo")]
        public async Task<ActionResult> AddReusablePartToPOST(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.LoadType(id);

            if (typeViewModel == null)
            {
                return NotFound();
            }

            var viewModel = new AddReusablePartViewModel();
            if (!await TryUpdateModelAsync(viewModel))
            {
                return await AddReusablePartTo(id);
            }

            viewModel.DisplayName = viewModel.DisplayName?.Trim() ?? String.Empty;
            viewModel.Name = viewModel.Name ?? String.Empty;

            if (String.IsNullOrWhiteSpace(viewModel.DisplayName))
            {
                ModelState.AddModelError("DisplayName", S["The Display Name can't be empty."]);
            }

            if (typeViewModel.TypeDefinition.Parts.Any(f => String.Equals(f.DisplayName().Trim(), viewModel.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("DisplayName", S["A part with the same Display Name already exists."]);
            }

            if (!String.IsNullOrWhiteSpace(viewModel.Name) && !viewModel.Name[0].IsLetter())
            {
                ModelState.AddModelError("Name", S["The Technical Name must start with a letter."]);
            }

            if (!String.Equals(viewModel.Name, viewModel.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Name", S["The Technical Name contains invalid characters."]);
            }

            if (String.IsNullOrWhiteSpace(viewModel.Name))
            {
                ModelState.AddModelError("Name", S["The Technical Name can't be empty."]);
            }

            if (typeViewModel.TypeDefinition.Parts.Any(f => String.Equals(f.Name.Trim(), viewModel.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Name", S["A part with the same Technical Name already exists."]);
            }

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();
                return await AddReusablePartTo(id);
            }

            var partToAdd = viewModel.SelectedPartName;

            _contentDefinitionService.AddReusablePartToType(viewModel.Name, viewModel.DisplayName, viewModel.Description, partToAdd, typeViewModel.Name);

            _notifier.Success(H["The \"{0}\" part has been added.", partToAdd]);

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("RemovePart")]
        public async Task<ActionResult> RemovePart(string id, string name)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeViewModel = _contentDefinitionService.LoadType(id);

            if (typeViewModel == null || !typeViewModel.TypeDefinition.Parts.Any(p => p.Name == name))
            {
                return NotFound();
            }

            _contentDefinitionService.RemovePartFromType(name, id);

            _notifier.Success(H["The \"{0}\" part has been removed.", name]);

            return RedirectToAction("Edit", new { id });
        }

        #endregion Types

        #region Parts

        public async Task<ActionResult> ListParts()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewContentTypes))
            {
                return Forbid();
            }

            return View(new ListContentPartsViewModel
            {
                // only user-defined parts (not code as they are not configurable)
                Parts = _contentDefinitionService.GetParts(true/*metadataPartsOnly*/)
            });
        }

        public async Task<ActionResult> CreatePart(string suggestion)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            return View(new CreatePartViewModel { Name = suggestion.ToSafeName() });
        }

        [HttpPost, ActionName("CreatePart")]
        public async Task<ActionResult> CreatePartPOST(CreatePartViewModel viewModel, string admin = null, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            viewModel.Name = viewModel.Name ?? String.Empty;

            if (String.IsNullOrWhiteSpace(viewModel.Name))
            {
                ModelState.AddModelError("Name", S["The Technical Name can't be empty."]);
            }

            if (_contentDefinitionService.LoadParts(false).Any(p => String.Equals(p.Name.Trim(), viewModel.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Name", S["A part with the same Technical Name already exists."]);
            }

            if (!String.IsNullOrWhiteSpace(viewModel.Name) && !viewModel.Name[0].IsLetter())
            {
                ModelState.AddModelError("Name", S["The Technical Name must start with a letter."]);
            }

            if (!String.Equals(viewModel.Name, viewModel.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Name", S["The Technical Name contains invalid characters."]);
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var partViewModel = _contentDefinitionService.AddPart(viewModel);

            if (partViewModel == null)
            {
                _notifier.Information(H["The content part could not be created."]);
                return View(viewModel);
            }

            _notifier.Success(H["The \"{0}\" content part has been created.", partViewModel.Name]);

            return RedirectToAction("EditPart", new { id = partViewModel.Name, admin = admin, returnUrl = returnUrl });
        }

        public async Task<ActionResult> EditPart(string id)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var contentPartDefinition = _contentDefinitionManager.GetPartDefinition(id);

            if (contentPartDefinition == null)
            {
                return NotFound();
            }

            var viewModel = new EditPartViewModel(contentPartDefinition);
            viewModel.Editor = await _contentDefinitionDisplayManager.BuildPartEditorAsync(contentPartDefinition, _updateModelAccessor.ModelUpdater);

            return View(viewModel);
        }

        [HttpPost, ActionName("EditPart")]
        [FormValueRequired("submit.Save")]
        public async Task<IActionResult> EditPartPOST(string id, string[] orderedFieldNames, string admin = null, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var contentPartDefinition = _contentDefinitionManager.LoadPartDefinition(id);

            if (contentPartDefinition == null)
            {
                return NotFound();
            }

            var viewModel = new EditPartViewModel(contentPartDefinition);
            viewModel.Editor = await _contentDefinitionDisplayManager.UpdatePartEditorAsync(contentPartDefinition, _updateModelAccessor.ModelUpdater);

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();
                return View(viewModel);
            }
            else
            {
                _contentDefinitionService.AlterPartFieldsOrder(contentPartDefinition, orderedFieldNames);
                _notifier.Success(H["The settings of \"{0}\" have been saved.", contentPartDefinition.Name]);
            }

            return RedirectToAction(nameof(EditPart), new { id, admin, returnUrl });
        }

        [HttpPost, ActionName("EditPart")]
        [FormValueRequired("submit.Delete")]
        public async Task<IActionResult> DeletePart(string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var partViewModel = _contentDefinitionService.LoadPart(id);

            if (partViewModel == null)
            {
                return NotFound();
            }

            _contentDefinitionService.RemovePart(id);

            _notifier.Information(H["\"{0}\" has been removed.", partViewModel.DisplayName]);

            return RedirectToReturnUrlOrAction(returnUrl, nameof(ListParts), null);
        }

        public async Task<ActionResult> AddFieldTo(string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var partViewModel = _contentDefinitionService.LoadPart(id);

            if (partViewModel == null)
            {
                return NotFound();
            }

            var viewModel = new AddFieldViewModel
            {
                Part = partViewModel.PartDefinition,
                Fields = _contentDefinitionService.GetFields().Select(x => x.Name).OrderBy(x => x).ToList()
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("AddFieldTo")]
        public async Task<ActionResult> AddFieldToPOST(AddFieldViewModel viewModel, string id, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var partViewModel = _contentDefinitionService.LoadPart(id);

            if (partViewModel == null)
            {
                return NotFound();
            }

            var partDefinition = partViewModel.PartDefinition;

            viewModel.DisplayName = viewModel.DisplayName?.Trim() ?? String.Empty;
            viewModel.Name = viewModel.Name ?? String.Empty;

            if (String.IsNullOrWhiteSpace(viewModel.DisplayName))
            {
                ModelState.AddModelError("DisplayName", S["The Display Name can't be empty."]);
            }

            if (partDefinition.Fields.Any(f => String.Equals(f.DisplayName().Trim(), viewModel.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("DisplayName", S["A field with the same Display Name already exists."]);
            }

            if (String.IsNullOrWhiteSpace(viewModel.Name))
            {
                ModelState.AddModelError("Name", S["The Technical Name can't be empty."]);
            }

            if (partDefinition.Fields.Any(f => String.Equals(f.Name.Trim(), viewModel.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Name", S["A field with the same Technical Name already exists."]);
            }

            if (!String.IsNullOrWhiteSpace(viewModel.Name) && !viewModel.Name[0].IsLetter())
            {
                ModelState.AddModelError("Name", S["The Technical Name must start with a letter."]);
            }

            if (!String.Equals(viewModel.Name, viewModel.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Name", S["The Technical Name contains invalid characters."]);
            }

            if (!ModelState.IsValid)
            {
                viewModel.Part = partDefinition;
                viewModel.Fields = _contentDefinitionService.GetFields().Select(x => x.Name).OrderBy(x => x).ToList();

                _documentStore.Cancel();

                return View(viewModel);
            }

            _contentDefinitionService.AddFieldToPart(viewModel.Name, viewModel.DisplayName, viewModel.FieldTypeName, partDefinition.Name);

            _notifier.Success(H["The field \"{0}\" has been added.", viewModel.DisplayName]);

            if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("EditField", new { id, viewModel.Name });
            }
        }

        public async Task<ActionResult> EditField(string id, string name, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var partViewModel = _contentDefinitionService.GetPart(id);

            if (partViewModel == null)
            {
                return NotFound();
            }

            var partFieldDefinition = partViewModel.PartDefinition.Fields.FirstOrDefault(x => x.Name == name);

            if (partFieldDefinition == null)
            {
                return NotFound();
            }

            var viewModel = new EditFieldViewModel
            {
                Name = partFieldDefinition.Name,
                Editor = partFieldDefinition.Editor(),
                DisplayMode = partFieldDefinition.DisplayMode(),
                DisplayName = partFieldDefinition.DisplayName(),
                PartFieldDefinition = partFieldDefinition,
                Shape = await _contentDefinitionDisplayManager.BuildPartFieldEditorAsync(partFieldDefinition, _updateModelAccessor.ModelUpdater)
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("EditField")]
        [FormValueRequired("submit.Save")]
        public async Task<ActionResult> EditFieldPOST(string id, EditFieldViewModel viewModel, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            if (viewModel == null)
            {
                return NotFound();
            }

            var partViewModel = _contentDefinitionService.LoadPart(id);

            if (partViewModel == null)
            {
                return NotFound();
            }

            var field = _contentDefinitionManager.LoadPartDefinition(id).Fields.FirstOrDefault(x => x.Name == viewModel.Name);

            if (field == null)
            {
                return NotFound();
            }

            viewModel.PartFieldDefinition = field;

            if (field.DisplayName() != viewModel.DisplayName)
            {
                // prevent null reference exception in validation
                viewModel.DisplayName = viewModel.DisplayName?.Trim() ?? String.Empty;

                if (String.IsNullOrWhiteSpace(viewModel.DisplayName))
                {
                    ModelState.AddModelError("DisplayName", S["The Display Name can't be empty."]);
                }

                if (_contentDefinitionService.LoadPart(partViewModel.Name).PartDefinition.Fields.Any(t => t.Name != viewModel.Name && String.Equals(t.DisplayName().Trim(), viewModel.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("DisplayName", S["A field with the same Display Name already exists."]);
                }

                if (!ModelState.IsValid)
                {
                    // Calls update to build editor shape with the display name validation failures, and other validation errors.
                    viewModel.Shape = await _contentDefinitionDisplayManager.UpdatePartFieldEditorAsync(field, _updateModelAccessor.ModelUpdater);
                    _documentStore.Cancel();

                    return View(viewModel);
                }

                _notifier.Information(H["Display name changed to {0}.", viewModel.DisplayName]);
            }

            _contentDefinitionService.AlterField(partViewModel, viewModel);

            // Refresh the local field variable in case it has been altered
            field = _contentDefinitionManager.LoadPartDefinition(id).Fields.FirstOrDefault(x => x.Name == viewModel.Name);

            viewModel.Shape = await _contentDefinitionDisplayManager.UpdatePartFieldEditorAsync(field, _updateModelAccessor.ModelUpdater);

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();

                return View(viewModel);
            }
            else
            {
                _notifier.Success(H["The \"{0}\" field settings have been saved.", field.DisplayName()]);
            }

            if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                // Redirect to the type editor if a type exists with this name
                var typeViewModel = _contentDefinitionService.LoadType(id);
                if (typeViewModel != null)
                {
                    return RedirectToAction("Edit", new { id });
                }

                return RedirectToAction("EditPart", new { id });
            }
        }

        [HttpPost, ActionName("RemoveFieldFrom")]
        public async Task<ActionResult> RemoveFieldFromPOST(string id, string name)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var partViewModel = _contentDefinitionService.LoadPart(id);

            if (partViewModel == null)
            {
                return NotFound();
            }

            var field = partViewModel.PartDefinition.Fields.FirstOrDefault(x => x.Name == name);

            if (field == null)
            {
                return NotFound();
            }

            _contentDefinitionService.RemoveFieldFromPart(name, partViewModel.Name);

            _notifier.Success(H["The \"{0}\" field has been removed.", field.DisplayName()]);

            if (_contentDefinitionService.LoadType(id) != null)
            {
                return RedirectToAction("Edit", new { id });
            }

            return RedirectToAction("EditPart", new { id });
        }

        #endregion Parts

        #region Type Parts

        public async Task<ActionResult> EditTypePart(string id, string name)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            var typeDefinition = _contentDefinitionManager.GetTypeDefinition(id);

            if (typeDefinition == null)
            {
                return NotFound();
            }

            var typePartDefinition = typeDefinition.Parts.FirstOrDefault(x => x.Name == name);

            if (typePartDefinition == null)
            {
                return NotFound();
            }

            var typePartViewModel = new EditTypePartViewModel
            {
                Name = typePartDefinition.Name,
                Editor = typePartDefinition.Editor(),
                DisplayMode = typePartDefinition.DisplayMode(),
                DisplayName = typePartDefinition.DisplayName(),
                Description = typePartDefinition.Description(),
                TypePartDefinition = typePartDefinition,
                Shape = await _contentDefinitionDisplayManager.BuildTypePartEditorAsync(typePartDefinition, _updateModelAccessor.ModelUpdater)
            };

            return View(typePartViewModel);
        }

        [HttpPost, ActionName("EditTypePart")]
        [FormValueRequired("submit.Save")]
        public async Task<IActionResult> EditTypePartPOST(string id, EditTypePartViewModel viewModel, string returnUrl = null)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContentTypes))
            {
                return Forbid();
            }

            if (viewModel == null)
            {
                return NotFound();
            }

            var typeDefinition = _contentDefinitionManager.LoadTypeDefinition(id);

            if (typeDefinition == null)
            {
                return NotFound();
            }

            var part = typeDefinition.Parts.FirstOrDefault(x => x.Name == viewModel.Name);

            if (part == null)
            {
                return NotFound();
            }

            viewModel.TypePartDefinition = part;

            if (part.PartDefinition.IsReusable())
            {
                if (part.DisplayName() != viewModel.DisplayName)
                {
                    // Prevent null reference exception in validation
                    viewModel.DisplayName = viewModel.DisplayName?.Trim() ?? String.Empty;

                    if (String.IsNullOrWhiteSpace(viewModel.DisplayName))
                    {
                        ModelState.AddModelError("DisplayName", S["The Display Name can't be empty."]);
                    }

                    if (typeDefinition.Parts.Any(t => t.Name != viewModel.Name && String.Equals(t.DisplayName()?.Trim(), viewModel.DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        ModelState.AddModelError("DisplayName", S["A part with the same Display Name already exists."]);
                    }

                    if (!ModelState.IsValid)
                    {
                        viewModel.Shape = await _contentDefinitionDisplayManager.UpdateTypePartEditorAsync(part, _updateModelAccessor.ModelUpdater);
                        _documentStore.Cancel();
                        return View(viewModel);
                    }
                }
            }

            _contentDefinitionService.AlterTypePart(viewModel);

            // Refresh the local part variable in case it has been altered
            part = _contentDefinitionManager.LoadTypeDefinition(id).Parts.FirstOrDefault(x => x.Name == viewModel.Name);

            viewModel.Shape = await _contentDefinitionDisplayManager.UpdateTypePartEditorAsync(part, _updateModelAccessor.ModelUpdater);

            if (!ModelState.IsValid)
            {
                _documentStore.Cancel();
                return View(viewModel);
            }
            else
            {
                _notifier.Success(H["The \"{0}\" part settings have been saved.", part.DisplayName()]);
            }

            return RedirectToReturnUrlOrAction(returnUrl, nameof(Edit), id);
        }

        #endregion Type Parts

        private IActionResult RedirectToReturnUrlOrAction(string returnUrl, string action, string id)
        {
            if ((String.IsNullOrEmpty(returnUrl) == false) && (Url.IsLocalUrl(returnUrl)))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(action, new { id });
            }
        }
    }
}
