using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using OpenIddict.Core;
using OrchardCore.OpenId.Abstractions.Models;
using OrchardCore.OpenId.Abstractions.Stores;
using OrchardCore.OpenId.Models;
using OrchardCore.OpenId.YesSql.Indexes;
using OrchardCore.OpenId.YesSql.Models;
using YesSql;

namespace OrchardCore.OpenId.YesSql.Services
{
    public class OpenIdApplicationStore : IOpenIdApplicationStore
    {
        private readonly ISession _session;

        public OpenIdApplicationStore(ISession session)
        {
            _session = session;
        }

        /// <summary>
        /// Determines the number of applications that exist in the database.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the number of applications in the database.
        /// </returns>
        public virtual async Task<long> CountAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _session.Query<OpenIdApplication>().CountAsync();
        }

        /// <summary>
        /// Determines the number of applications that match the specified query.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the number of applications that match the specified query.
        /// </returns>
        public virtual Task<long> CountAsync<TResult>(Func<IQueryable<IOpenIdApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <summary>
        /// Creates a new application.
        /// </summary>
        /// <param name="application">The application to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose result returns the application.
        /// </returns>
        public virtual async Task<IOpenIdApplication> CreateAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _session.Save(application);
            await _session.CommitAsync();

            return application;
        }

        /// <summary>
        /// Removes an existing application.
        /// </summary>
        /// <param name="application">The application to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task DeleteAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _session.Delete(application);

            return _session.CommitAsync();
        }

        /// <summary>
        /// Retrieves an application using its unique identifier.
        /// </summary>
        /// <param name="identifier">The unique identifier associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the client application corresponding to the identifier.
        /// </returns>
        public virtual async Task<IOpenIdApplication> FindByIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await _session.Query<OpenIdApplication, OpenIdApplicationIndex>(index => index.ApplicationId == identifier).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Retrieves an application using its client identifier.
        /// </summary>
        /// <param name="identifier">The client identifier associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the client application corresponding to the identifier.
        /// </returns>
        public virtual async Task<IOpenIdApplication> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await _session.Query<OpenIdApplication, OpenIdApplicationIndex>(index => index.ClientId == identifier).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Retrieves an application using its physical identifier.
        /// </summary>
        /// <param name="identifier">The unique identifier associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the client application corresponding to the identifier.
        /// </returns>
        public virtual async Task<IOpenIdApplication> FindByPhysicalIdAsync(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await _session.GetAsync<OpenIdApplication>(int.Parse(identifier, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Retrieves all the applications associated with the specified post_logout_redirect_uri.
        /// </summary>
        /// <param name="address">The post_logout_redirect_uri associated with the applications.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose result
        /// returns the client applications corresponding to the specified post_logout_redirect_uri.
        /// </returns>
        public virtual async Task<ImmutableArray<IOpenIdApplication>> FindByPostLogoutRedirectUriAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("The address cannot be null or empty.", nameof(address));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return ImmutableArray.CreateRange<IOpenIdApplication>(
                await _session.Query<OpenIdApplication, OpenIdApplicationByPostLogoutRedirectUriIndex>(
                    index => index.PostLogoutRedirectUri == address).ListAsync());
        }

        /// <summary>
        /// Retrieves all the applications associated with the specified redirect_uri.
        /// </summary>
        /// <param name="address">The redirect_uri associated with the applications.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose result
        /// returns the client applications corresponding to the specified redirect_uri.
        /// </returns>
        public virtual async Task<ImmutableArray<IOpenIdApplication>> FindByRedirectUriAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("The address cannot be null or empty.", nameof(address));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return ImmutableArray.CreateRange<IOpenIdApplication>(
                await _session.Query<OpenIdApplication, OpenIdApplicationByRedirectUriIndex>(
                    index => index.RedirectUri == address).ListAsync());
        }

        /// <summary>
        /// Executes the specified query and returns the first element.
        /// </summary>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="state">The optional state.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the first element returned when executing the query.
        /// </returns>
        public virtual Task<TResult> GetAsync<TState, TResult>(
            Func<IQueryable<IOpenIdApplication>, TState, IQueryable<TResult>> query,
            TState state, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <summary>
        /// Retrieves the client identifier associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the client identifier associated with the application.
        /// </returns>
        public virtual Task<string> GetClientIdAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(((OpenIdApplication) application).ClientId);
        }

        /// <summary>
        /// Retrieves the client secret associated with an application.
        /// Note: depending on the manager used to create the application,
        /// the client secret may be hashed for security reasons.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the client secret associated with the application.
        /// </returns>
        public virtual Task<string> GetClientSecretAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(((OpenIdApplication) application).ClientSecret);
        }

        /// <summary>
        /// Retrieves the client type associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the client type of the application (by default, "public").
        /// </returns>
        public virtual Task<string> GetClientTypeAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            // Optimization: avoid double-allocating strings for well-known values.
            switch (((OpenIdApplication) application).Type)
            {
                case ClientType.Confidential:
                    return Task.FromResult(OpenIddictConstants.ClientTypes.Confidential);

                case ClientType.Public:
                    return Task.FromResult(OpenIddictConstants.ClientTypes.Public);
            }

            return Task.FromResult(((OpenIdApplication) application).Type.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Retrieves the display name associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the display name associated with the application.
        /// </returns>
        public virtual Task<string> GetDisplayNameAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(((OpenIdApplication) application).DisplayName);
        }

        /// <summary>
        /// Retrieves the unique identifier associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the unique identifier associated with the application.
        /// </returns>
        public virtual Task<string> GetIdAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(((OpenIdApplication) application).ApplicationId);
        }

        /// <summary>
        /// Retrieves the physical identifier associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the physical identifier associated with the application.
        /// </returns>
        public virtual Task<string> GetPhysicalIdAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(((OpenIdApplication) application).Id.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Retrieves the logout callback addresses associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose
        /// result returns all the post_logout_redirect_uri associated with the application.
        /// </returns>
        public virtual Task<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(ImmutableArray.CreateRange(((OpenIdApplication) application).PostLogoutRedirectUris));
        }

        /// <summary>
        /// Retrieves the callback addresses associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns all the redirect_uri associated with the application.
        /// </returns>
        public virtual Task<ImmutableArray<string>> GetRedirectUrisAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(ImmutableArray.CreateRange(((OpenIdApplication) application).RedirectUris));
        }

        /// <summary>
        /// Instantiates a new application.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose result
        /// returns the instantiated application, that can be persisted in the database.
        /// </returns>
        public virtual Task<IOpenIdApplication> InstantiateAsync(CancellationToken cancellationToken)
            => Task.FromResult<IOpenIdApplication>(new OpenIdApplication { ApplicationId = Guid.NewGuid().ToString("n") });

        /// <summary>
        /// Executes the specified query and returns all the corresponding elements.
        /// </summary>
        /// <param name="count">The number of results to return.</param>
        /// <param name="offset">The number of results to skip.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns all the elements returned when executing the specified query.
        /// </returns>
        public virtual async Task<ImmutableArray<IOpenIdApplication>> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
        {
            var query = _session.Query<OpenIdApplication>();

            if (offset.HasValue)
            {
                query = query.Skip(offset.Value);
            }

            if (count.HasValue)
            {
                query = query.Take(count.Value);
            }

            return ImmutableArray.CreateRange<IOpenIdApplication>(await query.ListAsync());
        }

        /// <summary>
        /// Executes the specified query and returns all the corresponding elements.
        /// </summary>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="state">The optional state.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns all the elements returned when executing the specified query.
        /// </returns>
        public virtual Task<ImmutableArray<TResult>> ListAsync<TState, TResult>(
            Func<IQueryable<IOpenIdApplication>, TState, IQueryable<TResult>> query,
            TState state, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        /// <summary>
        /// Sets the client identifier associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="identifier">The client identifier associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task SetClientIdAsync(IOpenIdApplication application,
            string identifier, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).ClientId = identifier;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the client secret associated with an application.
        /// Note: depending on the manager used to create the application,
        /// the client secret may be hashed for security reasons.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="secret">The client secret associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task SetClientSecretAsync(IOpenIdApplication application, string secret, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).ClientSecret = secret;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the client type associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="type">The client type associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task SetClientTypeAsync(IOpenIdApplication application, string type, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (!Enum.TryParse(type, out ClientType value))
            {
                throw new ArgumentException("The specified client type is not valid.");
            }

            ((OpenIdApplication) application).Type = value;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the display name associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="name">The display name associated with the application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task SetDisplayNameAsync(IOpenIdApplication application, string name, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).DisplayName = name;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the logout callback addresses associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="addresses">The logout callback addresses associated with the application </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task SetPostLogoutRedirectUrisAsync(IOpenIdApplication application,
            ImmutableArray<string> addresses, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).PostLogoutRedirectUris = new HashSet<string>(addresses);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the callback addresses associated with an application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="addresses">The callback addresses associated with the application </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task SetRedirectUrisAsync(IOpenIdApplication application,
            ImmutableArray<string> addresses, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).RedirectUris = new HashSet<string>(addresses);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates an existing application.
        /// </summary>
        /// <param name="application">The application to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public virtual Task UpdateAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            cancellationToken.ThrowIfCancellationRequested();

            _session.Save(application);

            return _session.CommitAsync();
        }

        // TODO: remove these methods once per-application grant type limitation is added to OpenIddict.
        public virtual Task<ImmutableArray<string>> GetGrantTypesAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var builder = ImmutableArray.CreateBuilder<string>();

            var entity = (OpenIdApplication) application;
            if (entity.AllowAuthorizationCodeFlow)
            {
                builder.Add(OpenIdConnectConstants.GrantTypes.AuthorizationCode);
            }

            if (entity.AllowClientCredentialsFlow)
            {
                builder.Add(OpenIdConnectConstants.GrantTypes.ClientCredentials);
            }

            if (entity.AllowImplicitFlow)
            {
                builder.Add(OpenIdConnectConstants.GrantTypes.Implicit);
            }

            if (entity.AllowPasswordFlow)
            {
                builder.Add(OpenIdConnectConstants.GrantTypes.Password);
            }

            if (entity.AllowRefreshTokenFlow)
            {
                builder.Add(OpenIdConnectConstants.GrantTypes.RefreshToken);
            }

            return Task.FromResult(builder.ToImmutable());
        }

        public virtual Task SetGrantTypesAsync(IOpenIdApplication application, ImmutableArray<string> types, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var entity = (OpenIdApplication) application;

            entity.AllowAuthorizationCodeFlow = types.Contains(OpenIdConnectConstants.GrantTypes.AuthorizationCode);
            entity.AllowClientCredentialsFlow = types.Contains(OpenIdConnectConstants.GrantTypes.ClientCredentials);
            entity.AllowImplicitFlow = types.Contains(OpenIdConnectConstants.GrantTypes.Implicit);
            entity.AllowPasswordFlow = types.Contains(OpenIdConnectConstants.GrantTypes.Password);
            entity.AllowRefreshTokenFlow = types.Contains(OpenIdConnectConstants.GrantTypes.RefreshToken);

            return Task.CompletedTask;
        }

        public virtual Task<bool> IsConsentRequiredAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(!((OpenIdApplication) application).SkipConsent);
        }

        public virtual Task SetConsentRequiredAsync(IOpenIdApplication application, bool value, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).SkipConsent = !value;

            return Task.CompletedTask;
        }

        public virtual Task<ImmutableArray<string>> GetRolesAsync(IOpenIdApplication application, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(ImmutableArray.CreateRange(((OpenIdApplication) application).RoleNames));
        }

        public virtual async Task<ImmutableArray<IOpenIdApplication>> ListInRoleAsync(string role, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(role))
            {
                throw new ArgumentException("The role name cannot be null or empty.", nameof(role));
            }

            return ImmutableArray.CreateRange<IOpenIdApplication>(
                await _session.Query<OpenIdApplication, OpenIdApplicationByRoleNameIndex>(index => index.RoleName == role).ListAsync());
        }

        public virtual Task SetRolesAsync(IOpenIdApplication application, ImmutableArray<string> roles, CancellationToken cancellationToken)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            ((OpenIdApplication) application).RoleNames = new HashSet<string>(roles);
            _session.Save(application);

            return Task.CompletedTask;
        }
    }
}
