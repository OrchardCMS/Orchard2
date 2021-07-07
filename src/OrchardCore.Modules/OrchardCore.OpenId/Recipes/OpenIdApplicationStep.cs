using System;
using System.Linq;
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OrchardCore.OpenId.Abstractions.Descriptors;
using OrchardCore.OpenId.Abstractions.Managers;
using OrchardCore.OpenId.ViewModels;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.OpenId.Recipes
{
    public class OpenIdApplicationStep : IRecipeStepHandler
    {
        private readonly IOpenIdApplicationManager _applicationManager;

        /// <summary>
        /// This recipe step adds an OpenID Connect app.
        /// </summary>
        public OpenIdApplicationStep(IOpenIdApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!string.Equals(context.Name, "OpenIdApplication", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var model = context.Step.ToObject<OpenIdApplicationStepModel>();
            var application = await _applicationManager.FindByClientIdAsync(model.ClientId);

            var descriptor = new OpenIdApplicationDescriptor();
            if (application != null)
            {
                await _applicationManager.PopulateAsync(application, descriptor);

                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.Implicit);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.Password);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.Endpoints.Logout);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.Endpoints.Authorization);

                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.Code);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.IdToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                descriptor.Permissions.Remove(OpenIddictConstants.Permissions.ResponseTypes.Token);

                descriptor.RedirectUris.Clear();
                descriptor.PostLogoutRedirectUris.Clear();
                descriptor.Roles.Clear();
            }

            descriptor.ClientId = model.ClientId;
            descriptor.ClientSecret = model.ClientSecret;
            descriptor.ConsentType = model.ConsentType;
            descriptor.DisplayName = model.DisplayName;
            descriptor.Type = model.Type;

            if (model.AllowLogoutEndpoint)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);

                if (model.PostLogoutRedirectUris.Any())
                {
                    descriptor.PostLogoutRedirectUris.UnionWith(
                        from uri in model.PostLogoutRedirectUris
                        select new Uri(uri, UriKind.Absolute));
                }
            }
            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            }
            if (model.AllowClientCredentialsFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);

                if (model.Roles != null && model.Roles.Any())
                {
                    descriptor.Roles.UnionWith(model.Roles);
                }
            }
            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }
            if (model.AllowPasswordFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            }
            if (model.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }
            if (model.AllowAuthorizationCodeFlow || model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }
            if (model.AllowAuthorizationCodeFlow || model.AllowClientCredentialsFlow ||
                model.AllowPasswordFlow || model.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            }
            
            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }
            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }
            if (model.AllowHybridFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
                }
            }

            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }
            if (model.AllowImplicitFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }
            if (model.AllowHybridFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

                if (string.Equals(model.Type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
                }
            }

            if (model.AllowAuthorizationCodeFlow || model.AllowHybridFlow || model.AllowImplicitFlow)
            {
                if (model.RedirectUris.Any())
                {
                    descriptor.RedirectUris.UnionWith(
                    from uri in model.RedirectUris
                    select new Uri(uri, UriKind.Absolute));
                }
            }

            if (application != null)
            {
                await _applicationManager.UpdateAsync(application, descriptor);
            }
            else
            {
                await _applicationManager.CreateAsync(descriptor);
            }
        }
    }
}
