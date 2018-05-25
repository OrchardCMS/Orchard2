using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OrchardCore.Modules;

namespace Microsoft.AspNetCore.Builder
{
    public static class ModularApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseModules(this IApplicationBuilder app, Action<IApplicationBuilder> configure = null)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();

            env.ContentRootFileProvider = new CompositeFileProvider(
                new ModuleEmbeddedFileProvider(env),
                env.ContentRootFileProvider);

            // Ensure the shell tenants are loaded when a request comes in
            // and replaces the current service provider for the tenant's one.
            app.UseMiddleware<PoweredByMiddleware>();
            app.UseMiddleware<ModularTenantContainerMiddleware>();

            configure?.Invoke(app);

            app.UseMiddleware<ModularTenantRouterMiddleware>();

            return app;
        }
    }
}