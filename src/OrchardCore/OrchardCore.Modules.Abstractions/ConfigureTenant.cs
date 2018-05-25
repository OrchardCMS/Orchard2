using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OrchardCore.Modules
{
    public interface IConfigureTenant { }

    public class ConfigureTenant : StartupBase, IConfigureTenant
    {
        public ConfigureTenant(Action<IApplicationBuilder, IRouteBuilder, IServiceProvider> configure, int order)
        {
            configureAction = configure;
            Order = order;
        }

        public override int Order { get; }

        public Action<IApplicationBuilder, IRouteBuilder, IServiceProvider> configureAction { get; }

        public override void Configure(IApplicationBuilder app, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            configureAction?.Invoke(app, routes, serviceProvider);
        }
    }

    public class ConfigureTenantServices : StartupBase, IConfigureTenant
    {
        public ConfigureTenantServices(Action<IServiceCollection> configureServices, int order)
        {
            configureServicesAction = configureServices;
            Order = order;
        }

        public override int Order { get; }

        public Action<IServiceCollection> configureServicesAction { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            configureServicesAction?.Invoke(services);
        }
    }

    public class ConfigureTenantServices<TDep> : StartupBase, IConfigureTenant
        where TDep : class
    {
        public ConfigureTenantServices(TDep dependency, Action<IServiceCollection, TDep> configureServices, int order)
        {
            Dependency = dependency;
            configureServicesAction = configureServices;
            Order = order;
        }

        public override int Order { get; }

        public Action<IServiceCollection, TDep> configureServicesAction { get; }

        public TDep Dependency { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            configureServicesAction?.Invoke(services, Dependency);
        }
    }

    public class ConfigureTenantServices<TDep1, TDep2> : StartupBase, IConfigureTenant
        where TDep1 : class
        where TDep2: class
    {
        public ConfigureTenantServices(TDep1 dependency1, TDep2 dependency2,
            Action<IServiceCollection, TDep1, TDep2> configureServices, int order)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            configureServicesAction = configureServices;
            Order = order;
        }

        public override int Order { get; }

        public Action<IServiceCollection, TDep1, TDep2> configureServicesAction { get; }

        public TDep1 Dependency1 { get; }
        public TDep2 Dependency2 { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            configureServicesAction?.Invoke(services, Dependency1, Dependency2);
        }
    }

    public class ConfigureTenantServices<TDep1, TDep2, TDep3> : StartupBase, IConfigureTenant
        where TDep1 : class
        where TDep2 : class
        where TDep3 : class
    {
        public ConfigureTenantServices(TDep1 dependency1, TDep2 dependency2, TDep3 dependency3,
            Action<IServiceCollection, TDep1, TDep2, TDep3> configureServices, int order)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            configureServicesAction = configureServices;
            Order = order;
        }

        public override int Order { get; }

        public Action<IServiceCollection, TDep1, TDep2, TDep3> configureServicesAction { get; }

        public TDep1 Dependency1 { get; }
        public TDep2 Dependency2 { get; }
        public TDep3 Dependency3 { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            configureServicesAction?.Invoke(services, Dependency1, Dependency2, Dependency3);
        }
    }

    public class ConfigureTenantServices<TDep1, TDep2, TDep3, TDep4> : StartupBase, IConfigureTenant
        where TDep1 : class
        where TDep2 : class
        where TDep3 : class
        where TDep4 : class
    {
        public ConfigureTenantServices(TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4,
            Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4> configureServices, int order)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Dependency4 = dependency4;
            configureServicesAction = configureServices;
            Order = order;
        }

        public override int Order { get; }

        public Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4> configureServicesAction { get; }

        public TDep1 Dependency1 { get; }
        public TDep2 Dependency2 { get; }
        public TDep3 Dependency3 { get; }
        public TDep4 Dependency4 { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            configureServicesAction?.Invoke(services, Dependency1, Dependency2, Dependency3, Dependency4);
        }
    }

    public class ConfigureTenantServices<TDep1, TDep2, TDep3, TDep4, TDep5> : StartupBase, IConfigureTenant
        where TDep1 : class
        where TDep2 : class
        where TDep3 : class
        where TDep4 : class
        where TDep5 : class
    {
        public ConfigureTenantServices(TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5,
            Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4, TDep5> configureServices, int order)
        {
            Dependency1 = dependency1;
            Dependency2 = dependency2;
            Dependency3 = dependency3;
            Dependency4 = dependency4;
            Dependency5 = dependency5;
            configureServicesAction = configureServices;
            Order = order;
        }

        public override int Order { get; }

        public Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4, TDep5> configureServicesAction { get; }

        public TDep1 Dependency1 { get; }
        public TDep2 Dependency2 { get; }
        public TDep3 Dependency3 { get; }
        public TDep4 Dependency4 { get; }
        public TDep5 Dependency5 { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            configureServicesAction?.Invoke(services, Dependency1, Dependency2, Dependency3, Dependency4, Dependency5);
        }
    }

    public static class OrchardCoreBuilderExtensions
    {
        /// <summary>
        /// Configure the tenant pipeline before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenant(this OrchardCoreBuilder builder,
            Action<IApplicationBuilder, IRouteBuilder, IServiceProvider> configure, int order = int.MinValue)
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenant(configure, order));
            return builder;
        }

        /// <summary>
        /// Configure the tenant pipeline after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenant(this OrchardCoreBuilder builder,
            Action<IApplicationBuilder, IRouteBuilder, IServiceProvider> configure)
        {
            return builder.ConfigureTenant(configure, int.MaxValue);
        }

        /// <summary>
        /// Adds tenant level services before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenantServices(this OrchardCoreBuilder builder,
            Action<IServiceCollection> configureServices, int order = int.MinValue)
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenantServices(configureServices, order));
            return builder;
        }

        /// <summary>
        /// Adds tenant level services after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenantServices(this OrchardCoreBuilder builder,
            Action<IServiceCollection> configureServices)
        {
            return builder.ConfigureTenantServices(configureServices, int.MaxValue);
        }

        /// <summary>
        /// Adds tenant level services before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenantServices<TDep>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep> configureServices, int order = int.MinValue)
            where TDep : class
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenantServices<TDep>(
                sp.GetRequiredService<TDep>(),
                configureServices,
                order));

            return builder;
        }

        /// <summary>
        /// Adds tenant level services after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenantServices<TDep>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep> configureServices)
            where TDep : class
        {
            return builder.ConfigureTenantServices(configureServices, int.MaxValue);
        }

        /// <summary>
        /// Adds tenant level services before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenantServices<TDep1, TDep2>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2> configureServices, int order = int.MinValue)
            where TDep1 : class
            where TDep2 : class
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenantServices<TDep1, TDep2>(
                sp.GetRequiredService<TDep1>(),
                sp.GetRequiredService<TDep2>(),
                configureServices,
                order));

            return builder;
        }

        /// <summary>
        /// Adds tenant level services after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenantServices<TDep1, TDep2>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2> configureServices)
            where TDep1 : class
            where TDep2 : class
        {
            return builder.ConfigureTenantServices(configureServices, int.MaxValue);
        }

        /// <summary>
        /// Adds tenant level services before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenantServices<TDep1, TDep2, TDep3>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2, TDep3> configureServices, int order = int.MinValue)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenantServices<TDep1, TDep2, TDep3>(
                sp.GetRequiredService<TDep1>(),
                sp.GetRequiredService<TDep2>(),
                sp.GetRequiredService<TDep3>(),
                configureServices,
                order));

            return builder;
        }

        /// <summary>
        /// Adds tenant level services after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenantServices<TDep1, TDep2, TDep3>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2, TDep3> configureServices)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return builder.ConfigureTenantServices(configureServices, int.MaxValue);
        }

        /// <summary>
        /// Adds tenant level services before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenantServices<TDep1, TDep2, TDep3, TDep4>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4> configureServices, int order = int.MinValue)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenantServices<TDep1, TDep2, TDep3, TDep4>(
                sp.GetRequiredService<TDep1>(),
                sp.GetRequiredService<TDep2>(),
                sp.GetRequiredService<TDep3>(),
                sp.GetRequiredService<TDep4>(),
                configureServices,
                order));

            return builder;
        }

        /// <summary>
        /// Adds tenant level services after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenantServices<TDep1, TDep2, TDep3, TDep4>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4> configureServices)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
        {
            return builder.ConfigureTenantServices(configureServices, int.MaxValue);
        }

        /// <summary>
        /// Adds tenant level services before all modules (unless you specify a higher order).
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder ConfigureTenantServices<TDep1, TDep2, TDep3, TDep4, TDep5>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4, TDep5> configureServices, int order = int.MinValue)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
            where TDep5 : class
        {
            builder.Services.AddTransient<IStartup>(sp => new ConfigureTenantServices<TDep1, TDep2, TDep3, TDep4, TDep5>(
                sp.GetRequiredService<TDep1>(),
                sp.GetRequiredService<TDep2>(),
                sp.GetRequiredService<TDep3>(),
                sp.GetRequiredService<TDep4>(),
                sp.GetRequiredService<TDep5>(),
                configureServices,
                order));

            return builder;
        }

        /// <summary>
        /// Adds tenant level services after all modules.
        /// </summary>
        /// <param name="services"></param>
        public static OrchardCoreBuilder PostConfigureTenantServices<TDep1, TDep2, TDep3, TDep4, TDep5>(this OrchardCoreBuilder builder,
            Action<IServiceCollection, TDep1, TDep2, TDep3, TDep4, TDep5> configureServices)
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
            where TDep4 : class
            where TDep5 : class
        {
            return builder.ConfigureTenantServices(configureServices, int.MaxValue);
        }
    }
}
