using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors.ShapeTemplateStrategy;
using OrchardCore.DisplayManagement.Liquid;
using OrchardCore.DisplayManagement.Liquid.Filters;
using OrchardCore.DisplayManagement.Liquid.TagHelpers;
using OrchardCore.DisplayManagement.Liquid.Tags;
using OrchardCore.DisplayManagement.Razor;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.DisplayManagement.Zones;
using OrchardCore.Liquid;
using OrchardCore.Localization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrchardCoreBuilderExtensions
    {
        /// <summary>
        /// Adds tenant level services for managing liquid view template files.
        /// </summary>
        public static OrchardCoreBuilder AddLiquidViews(this OrchardCoreBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<LiquidViewParser>();
                services.AddSingleton<IAnchorTag, AnchorTag>();

                services.AddTransient<IConfigureOptions<TemplateOptions>, TemplateOptionsFileProviderSetup>();

                services.TryAddEnumerable(
                    ServiceDescriptor.Transient<IConfigureOptions<LiquidViewOptions>,
                    LiquidViewOptionsSetup>());

                services.TryAddEnumerable(
                    ServiceDescriptor.Transient<IConfigureOptions<ShapeTemplateOptions>,
                    LiquidShapeTemplateOptionsSetup>());

                services.AddSingleton<IApplicationFeatureProvider<ViewsFeature>, LiquidViewsFeatureProvider>();
                services.AddScoped<IRazorViewExtensionProvider, LiquidViewExtensionProvider>();
                services.AddSingleton<LiquidTagHelperFactory>();

                services.Configure<TemplateOptions>(o =>
                {
                    o.ValueConverters.Add(x =>
                    {
                        if (x is Shape s)
                        {
                            return new ObjectValue(s);
                        }
                        if (!(o is IShape) && o is IHtmlContent c)
                        {
                            return new HtmlContentValue(c);
                        }
                        return null;
                    });

                    o.MemberAccessStrategy.Register<Shape>("*", new ShapeAccessor());
                    o.MemberAccessStrategy.Register<ZoneHolding>("*", new ShapeAccessor());
                    o.MemberAccessStrategy.Register<ShapeMetadata>();

                    o.Scope.SetValue("Culture", new ObjectValue(new LiquidCultureAccessor()));
                    o.MemberAccessStrategy.Register<LiquidCultureAccessor, FluidValue>((obj, name, ctx) =>
                    {
                        return name switch
                        {
                            nameof(CultureInfo.Name) => new StringValue(CultureInfo.CurrentUICulture.Name),
                            "Dir" => new StringValue(CultureInfo.CurrentUICulture.GetLanguageDirection()),
                            _ => NilValue.Instance
                        };
                    });

                    o.Scope.SetValue("Request", new ObjectValue(new LiquidRequestAccessor()));
                    o.MemberAccessStrategy.Register<LiquidRequestAccessor, FluidValue>((obj, name, ctx) =>
                    {
                        var request = ((LiquidTemplateContext)ctx).Services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request;
                        if (request != null)
                        {
                            return name switch
                            {
                                nameof(HttpRequest.QueryString) => new StringValue(request.QueryString.Value),
                                nameof(HttpRequest.ContentType) => new StringValue(request.ContentType),
                                nameof(HttpRequest.ContentLength) => NumberValue.Create(request.ContentLength ?? 0),
                                nameof(HttpRequest.Cookies) => new ObjectValue(new CookieCollectionWrapper(request.Cookies)),
                                nameof(HttpRequest.Headers) => new ObjectValue(new HeaderDictionaryWrapper(request.Headers)),
                                nameof(HttpRequest.Query) => new ObjectValue(request.Query),
                                nameof(HttpRequest.Form) => request.HasFormContentType ? (FluidValue)new ObjectValue(request.Form) : NilValue.Instance,
                                nameof(HttpRequest.Protocol) => new StringValue(request.Protocol),
                                nameof(HttpRequest.Path) => new StringValue(request.Path.Value),
                                nameof(HttpRequest.PathBase) => new StringValue(request.PathBase.Value),
                                nameof(HttpRequest.Host) => new StringValue(request.Host.Value),
                                nameof(HttpRequest.IsHttps) => BooleanValue.Create(request.IsHttps),
                                nameof(HttpRequest.Scheme) => new StringValue(request.Scheme),
                                nameof(HttpRequest.Method) => new StringValue(request.Method),
                                nameof(HttpRequest.RouteValues) => new ObjectValue(new RouteValueDictionaryWrapper(request.RouteValues)),

                                // Provides correct escaping to reconstruct a request or redirect URI.
                                "UriHost" => new StringValue(request.Host.ToUriComponent(), encode: false),
                                "UriPath" => new StringValue(request.Path.ToUriComponent(), encode: false),
                                "UriPathBase" => new StringValue(request.PathBase.ToUriComponent(), encode: false),
                                "UriQueryString" => new StringValue(request.QueryString.ToUriComponent(), encode: false),

                                _ => NilValue.Instance
                            };
                        }

                        return NilValue.Instance;
                    });

                    o.Scope.SetValue("HttpContext", new ObjectValue(new LiquidHttpContextAccessor()));
                    o.MemberAccessStrategy.Register<LiquidHttpContextAccessor, FluidValue>((obj, name, ctx) =>
                    {
                        var httpContext = ((LiquidTemplateContext)ctx).Services.GetRequiredService<IHttpContextAccessor>().HttpContext;
                        if (httpContext != null)
                        {
                            return name switch
                            {
                                nameof(HttpContext.Items) => new ObjectValue(new HttpContextItemsWrapper(httpContext.Items)),
                                _ => NilValue.Instance
                            };
                        }

                        return NilValue.Instance;
                    });

                    o.MemberAccessStrategy.Register<FormCollection, FluidValue>((forms, name) =>
                    {
                        if (name == "Keys")
                        {
                            return new ArrayValue(forms.Keys.Select(x => new StringValue(x)));
                        }

                        return new ArrayValue(forms[name].Select(x => new StringValue(x)).ToArray());
                    });

                    o.MemberAccessStrategy.Register<HttpContextItemsWrapper, object>((httpContext, name) => httpContext.Items[name]);
                    o.MemberAccessStrategy.Register<QueryCollection, string[]>((queries, name) => queries[name].ToArray());
                    o.MemberAccessStrategy.Register<CookieCollectionWrapper, string>((cookies, name) => cookies.RequestCookieCollection[name]);
                    o.MemberAccessStrategy.Register<HeaderDictionaryWrapper, string[]>((headers, name) => headers.HeaderDictionary[name].ToArray());
                    o.MemberAccessStrategy.Register<RouteValueDictionaryWrapper, object>((headers, name) => headers.RouteValueDictionary[name]);

                })
                .AddLiquidFilter<AppendVersionFilter>("append_version")
                .AddLiquidFilter<ResourceUrlFilter>("resource_url")
                .AddLiquidFilter<SanitizeHtmlFilter>("sanitize_html");
            });

            return builder;
        }

        private class CookieCollectionWrapper
        {
            public readonly IRequestCookieCollection RequestCookieCollection;

            public CookieCollectionWrapper(IRequestCookieCollection requestCookieCollection)
            {
                RequestCookieCollection = requestCookieCollection;
            }
        }

        private class HeaderDictionaryWrapper
        {
            public readonly IHeaderDictionary HeaderDictionary;

            public HeaderDictionaryWrapper(IHeaderDictionary headerDictionary)
            {
                HeaderDictionary = headerDictionary;
            }
        }

        private class HttpContextItemsWrapper
        {
            public readonly IDictionary<object, object> Items;

            public HttpContextItemsWrapper(IDictionary<object, object> items)
            {
                Items = items;
            }
        }

        private class RouteValueDictionaryWrapper
        {
            public readonly IReadOnlyDictionary<string, object> RouteValueDictionary;

            public RouteValueDictionaryWrapper(IReadOnlyDictionary<string, object> routeValueDictionary)
            {
                RouteValueDictionary = routeValueDictionary;
            }
        }
    }
}
