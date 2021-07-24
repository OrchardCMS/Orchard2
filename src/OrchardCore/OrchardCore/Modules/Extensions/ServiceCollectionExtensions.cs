using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using OrchardCore;
using OrchardCore.Environment.Extensions;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Environment.Shell.Descriptor.Models;
using OrchardCore.Environment.Shell.Models;
using OrchardCore.Localization;
using OrchardCore.Locking;
using OrchardCore.Locking.Distributed;
using OrchardCore.Modules;
using OrchardCore.Modules.FileProviders;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OrchardCore services to the host service collection.
        /// </summary>
        public static OrchardCoreBuilder AddOrchardCore(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // If an instance of OrchardCoreBuilder exists reuse it,
            // so we can call AddOrchardCore several times.
            var builder = services
                .LastOrDefault(d => d.ServiceType == typeof(OrchardCoreBuilder))?
                .ImplementationInstance as OrchardCoreBuilder;

            if (builder == null)
            {
                builder = new OrchardCoreBuilder(services);
                services.AddSingleton(builder);

                AddDefaultServices(builder);
                AddShellServices(builder);
                AddExtensionServices(builder);
                AddStaticFiles(builder);

                AddRouting(builder);
                AddAntiForgery(builder);
                AddSameSiteCookieBackwardsCompatibility(builder);
                AddAuthentication(builder);
                AddDataProtection(builder);

                // Register the list of services to be resolved later on
                services.AddSingleton(services);
            }

            return builder;
        }

        /// <summary>
        /// Adds OrchardCore services to the host service collection and let the app change
        /// the default behavior and set of features through a configure action.
        /// </summary>
        public static IServiceCollection AddOrchardCore(this IServiceCollection services, Action<OrchardCoreBuilder> configure)
        {
            var builder = services.AddOrchardCore();

            configure?.Invoke(builder);

            return services;
        }

        private static void AddDefaultServices(OrchardCoreBuilder builder)
        {
            var services = builder.ApplicationServices;

            services.AddLogging();
            services.AddOptions();

            // These services might be moved at a higher level if no components from OrchardCore needs them.
            services.AddLocalization();

            // For performance, prevents the 'ResourceManagerStringLocalizer' from being used.
            // Also support pluralization.
            services.AddSingleton<IStringLocalizerFactory, NullStringLocalizerFactory>();
            services.AddSingleton<IHtmlLocalizerFactory, NullHtmlLocalizerFactory>();

            services.AddWebEncoders();

            services.AddHttpContextAccessor();
            services.AddSingleton<IClock, Clock>();
            services.AddScoped<ILocalClock, LocalClock>();

            services.AddScoped<ILocalizationService, DefaultLocalizationService>();
            services.AddScoped<ICalendarManager, DefaultCalendarManager>();
            services.AddScoped<ICalendarSelector, DefaultCalendarSelector>();

            services.AddSingleton<IPoweredByMiddlewareOptions, PoweredByMiddlewareOptions>();

            services.AddScoped<IOrchardHelper, DefaultOrchardHelper>();

            builder.ConfigureServices(s =>
            {
                s.AddSingleton<LocalLock>();
                s.AddSingleton<ILocalLock>(sp => sp.GetRequiredService<LocalLock>());
                s.AddSingleton<IDistributedLock>(sp => sp.GetRequiredService<LocalLock>());
            });
        }

        private static void AddShellServices(OrchardCoreBuilder builder)
        {
            var services = builder.ApplicationServices;

            // Use a single tenant and all features by default
            services.AddHostingShellServices();
            services.AddAllFeaturesDescriptor();

            // Registers the application primary feature.
            services.AddTransient(sp => new ShellFeature
            (
                sp.GetRequiredService<IHostEnvironment>().ApplicationName, alwaysEnabled: true)
            );

            // Registers the application default feature.
            services.AddTransient(sp => new ShellFeature
            (
                Application.DefaultFeatureId, alwaysEnabled: true)
            );

            builder.ConfigureServices(shellServices =>
            {
                shellServices.AddTransient<IConfigureOptions<ShellContextOptions>, ShellContextOptionsSetup>();
            });
        }

        private static void AddExtensionServices(OrchardCoreBuilder builder)
        {
            builder.ApplicationServices.AddSingleton<IModuleNamesProvider, AssemblyAttributeModuleNamesProvider>();
            builder.ApplicationServices.AddSingleton<IApplicationContext, ModularApplicationContext>();

            builder.ApplicationServices.AddExtensionManagerHost();

            builder.ConfigureServices(services =>
            {
                services.AddExtensionManager();
                services.AddExtensionValidation();
            });
        }

        /// <summary>
        /// Adds tenant level configuration to serve static files from modules
        /// </summary>
        private static void AddStaticFiles(OrchardCoreBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IModuleStaticFileProvider>(serviceProvider =>
                {
                    var env = serviceProvider.GetRequiredService<IHostEnvironment>();
                    var appContext = serviceProvider.GetRequiredService<IApplicationContext>();

                    IModuleStaticFileProvider fileProvider;
                    if (env.IsDevelopment())
                    {
                        var fileProviders = new List<IStaticFileProvider>
                        {
                            new ModuleProjectStaticFileProvider(appContext),
                            new ModuleEmbeddedStaticFileProvider(appContext)
                        };
                        fileProvider = new ModuleCompositeStaticFileProvider(fileProviders);
                    }
                    else
                    {
                        fileProvider = new ModuleEmbeddedStaticFileProvider(appContext);
                    }
                    return fileProvider;
                });

                services.AddSingleton<IStaticFileProvider>(serviceProvider =>
                {
                    return serviceProvider.GetRequiredService<IModuleStaticFileProvider>();
                });
            });

            builder.Configure((app, routes, serviceProvider) =>
            {
                var fileProvider = serviceProvider.GetRequiredService<IModuleStaticFileProvider>();

                var options = serviceProvider.GetRequiredService<IOptions<StaticFileOptions>>().Value;

                options.RequestPath = "";
                options.FileProvider = fileProvider;

                var shellConfiguration = serviceProvider.GetRequiredService<IShellConfiguration>();

                var cacheControl = shellConfiguration.GetValue("StaticFileOptions:CacheControl", "public, max-age=2592000, s-max-age=31557600");

                // Cache static files for a year as they are coming from embedded resources and should not vary
                options.OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = cacheControl;
                };

                app.UseStaticFiles(options);
            });
        }

        /// <summary>
        /// Adds isolated tenant level routing services.
        /// </summary>
        private static void AddRouting(OrchardCoreBuilder builder)
        {
            // 'AddRouting()' is called by the host.

            builder.ConfigureServices(collection =>
            {
                // The routing system is not tenant aware and uses a global list of endpoint data sources which is
                // setup by the default configuration of 'RouteOptions' and mutated on each call of 'UseEndPoints()'.
                // So, we need isolated routing singletons (and a default configuration) per tenant.

                var implementationTypesToRemove = new ServiceCollection().AddRouting()
                    .Where(sd => sd.Lifetime == ServiceLifetime.Singleton || sd.ServiceType == typeof(IConfigureOptions<RouteOptions>))
                    .Select(sd => sd.GetImplementationType())
                    .ToArray();

                var descriptorsToRemove = collection
                    .Where(sd => (sd is ClonedSingletonDescriptor || sd.ServiceType == typeof(IConfigureOptions<RouteOptions>)) &&
                        implementationTypesToRemove.Contains(sd.GetImplementationType()))
                    .ToArray();

                foreach (var descriptor in descriptorsToRemove)
                {
                    collection.Remove(descriptor);
                }

                collection.AddRouting();
            },
            order: int.MinValue + 100);
        }

        /// <summary>
        /// Adds host and tenant level antiforgery services.
        /// </summary>
        private static void AddAntiForgery(OrchardCoreBuilder builder)
        {
            builder.ApplicationServices.AddAntiforgery();

            builder.ConfigureServices((services, serviceProvider) =>
            {
                var settings = serviceProvider.GetRequiredService<ShellSettings>();
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

                var cookieName = "orchantiforgery_" + HttpUtility.UrlEncode(settings.Name + environment.ContentRootPath);

                // If uninitialized, we use the host services.
                if (settings.State == TenantState.Uninitialized)
                {
                    // And delete a cookie that may have been created by another instance.
                    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

                    // Use case when creating a container without ambient context.
                    if (httpContextAccessor.HttpContext == null)
                    {
                        return;
                    }

                    // Use case when creating a container in a deferred task.
                    if (httpContextAccessor.HttpContext.Response.HasStarted)
                    {
                        return;
                    }

                    httpContextAccessor.HttpContext.Response.Cookies.Delete(cookieName);

                    return;
                }

                // Re-register the antiforgery  services to be tenant-aware.
                var collection = new ServiceCollection()
                    .AddAntiforgery(options =>
                    {
                        options.Cookie.Name = cookieName;

                        // Don't set the cookie builder 'Path' so that it uses the 'IAuthenticationFeature' value
                        // set by the pipeline and comming from the request 'PathBase' which already ends with the
                        // tenant prefix but may also start by a path related e.g to a virtual folder.
                    });

                services.Add(collection);
            });
        }

        /// <summary>
        /// Adds backwards compatibility to the handling of SameSite cookies.
        /// </summary>
        private static void AddSameSiteCookieBackwardsCompatibility(OrchardCoreBuilder builder)
        {
            builder.ConfigureServices(services =>
                {
                    services.Configure<CookiePolicyOptions>(options =>
                    {
                        options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                        options.OnAppendCookie = cookieContext => CheckSameSiteBackwardsCompatiblity(cookieContext.Context, cookieContext.CookieOptions);
                        options.OnDeleteCookie = cookieContext => CheckSameSiteBackwardsCompatiblity(cookieContext.Context, cookieContext.CookieOptions);
                    });
                })
                .Configure(app =>
                {
                    app.UseCookiePolicy();
                });
        }

        private static void CheckSameSiteBackwardsCompatiblity(HttpContext httpContext, CookieOptions options)
        {
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

            if (options.SameSite == SameSiteMode.None)
            {
                if (string.IsNullOrEmpty(userAgent))
                {
                    return;
                }

                // Cover all iOS based browsers here. This includes:
                // - Safari on iOS 12 for iPhone, iPod Touch, iPad
                // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
                // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
                // All of which are broken by SameSite=None, because they use the iOS networking stack
                if (userAgent.Contains("CPU iPhone OS 12") || userAgent.Contains("iPad; CPU OS 12"))
                {
                    options.SameSite = AspNetCore.Http.SameSiteMode.Unspecified;
                    return;
                }

                // Cover Mac OS X based browsers that use the Mac OS networking stack. This includes:
                // - Safari on Mac OS X.
                // This does not include:
                // - Chrome on Mac OS X
                // Because they do not use the Mac OS networking stack.
                if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                    userAgent.Contains("Version/") && userAgent.Contains("Safari"))
                {
                    options.SameSite = AspNetCore.Http.SameSiteMode.Unspecified;
                    return;
                }

                // Cover Chrome 50-69, because some versions are broken by SameSite=None,
                // and none in this range require it.
                // Note: this covers some pre-Chromium Edge versions,
                // but pre-Chromium Edge does not require SameSite=None.
                if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
                {
                    options.SameSite = AspNetCore.Http.SameSiteMode.Unspecified;
                }
            }
        }

        /// <summary>
        /// Adds host and tenant level authentication services and configuration.
        /// </summary>
        private static void AddAuthentication(OrchardCoreBuilder builder)
        {
            builder.ApplicationServices.AddAuthentication();

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication();

                // IAuthenticationSchemeProvider is already registered at the host level.
                // We need to register it again so it is taken into account at the tenant level
                // because it holds a reference to an underlying dictionary, responsible of storing
                // the registered schemes which need to be distinct for each tenant.
                services.AddSingleton<IAuthenticationSchemeProvider, AuthenticationSchemeProvider>();
            })
            .Configure(app =>
            {
                app.UseAuthentication();
            });
        }

        /// <summary>
        /// Adds tenant level data protection services.
        /// </summary>
        private static void AddDataProtection(OrchardCoreBuilder builder)
        {
            builder.ConfigureServices((services, serviceProvider) =>
            {
                var settings = serviceProvider.GetRequiredService<ShellSettings>();
                var options = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();

                // The 'FileSystemXmlRepository' will create the directory, but only if it is not overridden.
                var directory = new DirectoryInfo(Path.Combine(
                    options.Value.ShellsApplicationDataPath,
                    options.Value.ShellsContainerName,
                    settings.Name, "DataProtection-Keys"));

                // Re-register the data protection services to be tenant-aware so that modules that internally
                // rely on IDataProtector/IDataProtectionProvider automatically get an isolated instance that
                // manages its own key ring and doesn't allow decrypting payloads encrypted by another tenant.
                // By default, the key ring is stored in the tenant directory of the configured App_Data path.
                var collection = new ServiceCollection()
                    .AddDataProtection()
                    .PersistKeysToFileSystem(directory)
                    .SetApplicationName(settings.Name)
                    .AddKeyManagementOptions(o => o.XmlEncryptor = o.XmlEncryptor ?? new NullXmlEncryptor())
                    .Services;

                // Remove any previously registered options setups.
                services.RemoveAll<IConfigureOptions<KeyManagementOptions>>();
                services.RemoveAll<IConfigureOptions<DataProtectionOptions>>();

                services.Add(collection);
            });
        }
    }
}
