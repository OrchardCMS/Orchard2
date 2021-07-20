using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Security.Permissions;
using System.Collections.Generic;

namespace OrchardCore.Profile.Navigation
{
    public class ProfileMenuItem
    {
        public ProfileMenuItem()
        {
            Permissions = new List<Permission>();
            Classes = new List<string>();
            Items = new List<ProfileMenuItem>();
            LinkToFirstChild = true;
        }

        /// <summary>
        /// The text to display with the menu item.
        /// </summary>
        public LocalizedString Text { get; set; }

        /// <summary>
        /// The html id of the menu item.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The final url that the menu item will link to. This property is computed
        /// based on <see cref="Url"/> or <see cref="RouteValues"/>.
        /// </summary>
        public string Href { get; set; }

        /// <summary>
        /// The optional url the menu item should link to.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The relative position of the menu item among its siblings. e.g., 10, 0, "after".
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Whether the menu item should link to the same url as its first child.
        /// </summary>
        public bool LinkToFirstChild { get; set; }

        /// <summary>
        /// <c>True</c> if the menu item is local to the page, like a tab.
        /// </summary>
        public bool LocalNav { get; set; }

        /// <summary>
        /// The culture for which this menu item is used.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The resource the permission is protecting.
        /// </summary>
        public object Resource { get; set; }

        /// <summary>
        /// The child menu items.
        /// </summary>
        public List<ProfileMenuItem> Items { get; set; }

        /// <summary>
        /// The optional route values for this menu item.
        /// </summary>
        public RouteValueDictionary RouteValues { get; set; }

        /// <summary>
        /// The list of <see cref="Permission"/> objects the user must have in order to see this menu item.
        /// </summary>
        public List<Permission> Permissions { get; }

        /// <summary>
        /// The css classes to render with the menu item.
        /// </summary>
        public List<string> Classes { get; }
    }
}
