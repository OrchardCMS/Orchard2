using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Profile.Navigation;

namespace OrchardCore.Profile
{
    public class ProfileNavigationShapes : IShapeTableProvider
    {
        public void Discover(ShapeTableBuilder builder)
        {
            builder.Describe("ProfileNavigation")
                .OnDisplaying(displaying =>
                {
                    dynamic menu = displaying.Shape;
                    string identifier = menu.ContentItemId ?? menu.Alias;
                    if (String.IsNullOrEmpty(identifier))
                    {
                        return;
                    }

                    menu.Classes.Add("menu-" + identifier.HtmlClassify());
                    menu.Classes.Add("menu");
                    menu.Metadata.Alternates.Add("ProfileNavigation__" + EncodeAlternateElement(identifier));
                })
                .OnProcessing(async context =>
                {
                    dynamic menu = context.Shape;
                    string menuName = menu.MenuName;

                    // Menu population is executed when processing the shape so that its value
                    // can be cached. IShapeDisplayEvents is called before the ShapeDescriptor
                    // events and thus this code can be cached.

                    if ((bool)menu.HasItems)
                    {
                        return;
                    }

                    var viewContextAccessor = context.ServiceProvider.GetRequiredService<ViewContextAccessor>();
                    var viewContext = viewContextAccessor.ViewContext;

                    var navigationManager = context.ServiceProvider.GetRequiredService<IProfileNavigationManager>();
                    var shapeFactory = context.ServiceProvider.GetRequiredService<IShapeFactory>();
                    var httpContextAccessor = context.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    var menuItems = navigationManager.BuildMenu(menuName, viewContext);
                    var httpContext = httpContextAccessor.HttpContext;

                    if (httpContext != null)
                    {
                        // adding query string parameters
                        var route = menu.RouteData;
                        var routeData = new RouteValueDictionary(route.Values);
                        var query = httpContext.Request.Query;

                        if (query != null)
                        {
                            foreach (var pair in query)
                            {
                                if (pair.Key != null && !routeData.ContainsKey(pair.Key))
                                {
                                    routeData[pair.Key] = pair.Value;
                                }
                            }
                        }
                    }

                    // TODO: Flag Selected menu item
                    await ProfileNavigationHelper.PopulateMenuAsync(shapeFactory, menu, menu, menuItems, viewContext);
                });

            builder.Describe("ProfileNavigationItem")
                .OnDisplaying(displaying =>
                {
                    dynamic menuItem = displaying.Shape;
                    var menu = menuItem.Menu;
                    string menuName = menu.MenuName;
                    int level = menuItem.Level;
                    string differentiator = menuItem.Differentiator;
                    menuItem.Metadata.Alternates.Add("ProfileNavigationItem__level__" + level);
                    menuItem.Metadata.Alternates.Add("ProfileNavigationItem__" + EncodeAlternateElement(menuName));
                    menuItem.Metadata.Alternates.Add("ProfileNavigationItem__" + EncodeAlternateElement(menuName) + "__level__" + level);
                });

            builder.Describe("ProfileNavigationItemLink")
                .OnDisplaying(displaying =>
                {
                    dynamic menuItem = displaying.Shape;
                    string menuName = menuItem.Menu.MenuName;
                    int level = menuItem.Level;

                    menuItem.Metadata.Alternates.Add("ProfileNavigationItemLink__level__" + level);

                    // NavigationItemLink__[MenuName] e.g. NavigationItemLink-Main-Menu
                    // NavigationItemLink__[MenuName]__level__[level] e.g. NavigationItemLink-Main-Menu-level-2
                    menuItem.Metadata.Alternates.Add("ProfileNavigationItemLink__" + EncodeAlternateElement(menuName));
                    menuItem.Metadata.Alternates.Add("ProfileNavigationItemLink__" + EncodeAlternateElement(menuName) + "__level__" + level);
                });
        }

        /// <summary>
        /// Encodes dashed and dots so that they don't conflict in filenames
        /// </summary>
        /// <param name="alternateElement"></param>
        /// <returns></returns>
        private string EncodeAlternateElement(string alternateElement)
        {
            return alternateElement.Replace("-", "__").Replace(".", "_");
        }
    }
}
