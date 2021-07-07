using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Environment.Shell;
using OrchardCore.OpenId.Abstractions.Descriptors;
using OrchardCore.OpenId.Abstractions.Managers;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.OpenId.Recipes
{
    public class OpenIdScopeStep : IRecipeStepHandler
    {
        private readonly IOpenIdScopeManager _scopeManager;
        private readonly ShellSettings _shellSettings;

        /// <summary>
        /// This recipe step adds an OpenID Connect scope.
        /// </summary>
        public OpenIdScopeStep(IOpenIdScopeManager scopeManager, ShellSettings shellSettings)
        {
            _scopeManager = scopeManager;
            _shellSettings = shellSettings;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!string.Equals(context.Name, "OpenIdScope", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var model = context.Step.ToObject<OpenIdScopeStepModel>();
            var descriptor = new OpenIdScopeDescriptor();

            var scope = await _scopeManager.FindByNameAsync(model.ScopeName);
            if (scope != null)
            {
                await _scopeManager.PopulateAsync(scope, descriptor);
                descriptor.Resources.Clear();
            }

            descriptor.Description = model.Description;
            descriptor.Name = model.ScopeName;
            descriptor.DisplayName = model.DisplayName;

            if (model.Resources != null && model.Resources.Any())
            {
                descriptor.Resources.UnionWith(model.Resources);
            }

            if (model.TenantNames != null && model.TenantNames.Any())
            {
                descriptor.Resources.UnionWith(model.TenantNames
                .Where(tenantName => !string.Equals(tenantName, _shellSettings.Name))
                .Select(tenantName => OpenIdConstants.Prefixes.Tenant + tenantName));
            }

            if (scope != null)
            {
                await _scopeManager.UpdateAsync(scope, descriptor);
            }
            else
            {
                await _scopeManager.CreateAsync(descriptor);
            }  
        }
    }
}
