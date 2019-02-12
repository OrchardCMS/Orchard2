using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OrchardCore.Modules;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Enables multi-tenant requests support for the current path.
        /// </summary>
        public static IApplicationBuilder UseOrchardCore(this IApplicationBuilder app, Action<IApplicationBuilder> configure = null)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            var appContext = app.ApplicationServices.GetRequiredService<IApplicationContext>();

            env.ContentRootFileProvider = new CompositeFileProvider(
                new ModuleEmbeddedFileProvider(appContext),
                env.ContentRootFileProvider);

            var fileProviders = new List<IFileProvider>();
            fileProviders.Add(new ModuleEmbeddedStaticFileProvider(appContext));
            fileProviders.Add(env.WebRootFileProvider);

            if (env.IsDevelopment())
            {
                fileProviders.Insert(0, new ModuleProjectStaticFileProvider(appContext));
            }

            env.WebRootFileProvider = new CompositeFileProvider(fileProviders);

            app.UseMiddleware<PoweredByMiddleware>();

            // Ensure the shell tenants are loaded when a request comes in
            // and replaces the current service provider for the tenant's one.
            app.UseMiddleware<ModularTenantContainerMiddleware>();

            configure?.Invoke(app);

            app.UseMiddleware<ModularTenantRouterMiddleware>(app.ServerFeatures);

            return app;
        }
    }
}