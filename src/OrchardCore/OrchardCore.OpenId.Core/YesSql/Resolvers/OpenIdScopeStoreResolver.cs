/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OrchardCore.OpenId.YesSql.Models;
using OrchardCore.OpenId.YesSql.Stores;

namespace OrchardCore.OpenId.YesSql.Resolvers
{
    /// <summary>
    /// Exposes a method allowing to resolve a scope store.
    /// </summary>
    public class OpenIdScopeStoreResolver : IOpenIddictScopeStoreResolver
    {
        private readonly TypeResolutionCache _cache;
        private readonly IServiceProvider _provider;

        public OpenIdScopeStoreResolver(TypeResolutionCache cache, IServiceProvider provider)
        {
            _cache = cache;
            _provider = provider;
        }

        /// <summary>
        /// Returns a scope store compatible with the specified scope type or throws an
        /// <see cref="InvalidOperationException"/> if no store can be built using the specified type.
        /// </summary>
        /// <typeparam name="TScope">The type of the Scope entity.</typeparam>
        /// <returns>An <see cref="IOpenIddictScopeStore{TScope}"/>.</returns>
        public IOpenIddictScopeStore<TScope> Get<TScope>() where TScope : class
        {
            var store = _provider.GetService<IOpenIddictScopeStore<TScope>>();
            if (store != null)
            {
                return store;
            }

            var type = _cache.GetOrAdd(typeof(TScope), key =>
            {
                if (!typeof(OpenIdScope).IsAssignableFrom(key))
                {
                    throw new InvalidOperationException(new StringBuilder()
                        .AppendLine("The specified scope type is not compatible with the YesSql stores.")
                        .Append("When enabling the YesSql stores, make sure you use the built-in 'OpenIdScope' ")
                        .Append("entity (from the 'OrchardCore.OpenId.Core' package) or a custom entity ")
                        .Append("that inherits from the 'OpenIdScope' entity.")
                        .ToString());
                }

                return typeof(OpenIdScopeStore<>).MakeGenericType(key);
            });

            return (IOpenIddictScopeStore<TScope>) _provider.GetRequiredService(type);
        }

        // Note: OrchardCore YesSql resolvers are registered as scoped dependencies as their inner
        // service provider must be able to resolve scoped services (typically, the store they return).
        // To avoid having to declare a static type resolution cache, a special cache service is used
        // here and registered as a singleton dependency so that its content persists beyond the scope.
        public class TypeResolutionCache : ConcurrentDictionary<Type, Type> { }
    }
}
