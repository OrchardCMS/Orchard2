using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Profile.Navigation
{

    public class ProfileNavigationItemBuilder : ProfileNavigationBuilder
    {
        private readonly ProfileMenuItem _item;

        public ProfileNavigationItemBuilder()
        {
            _item = new ProfileMenuItem();
        }

        public ProfileNavigationItemBuilder(ProfileMenuItem existingItem)
        {
            if (existingItem == null)
            {
                throw new ArgumentNullException(nameof(existingItem));
            }

            _item = existingItem;
        }

        public ProfileNavigationItemBuilder Caption(LocalizedString caption)
        {
            _item.Text = caption;
            return this;
        }

        public ProfileNavigationItemBuilder Position(string position)
        {
            _item.Position = position;
            return this;
        }

        public ProfileNavigationItemBuilder Url(string url)
        {
            _item.Url = url;
            return this;
        }

        public ProfileNavigationItemBuilder Culture(string culture)
        {
            _item.Culture = culture;
            return this;
        }

        public ProfileNavigationItemBuilder Id(string id)
        {
            _item.Id = id;
            return this;
        }

        public ProfileNavigationItemBuilder AddClass(string className)
        {
            if (!_item.Classes.Contains(className))
                _item.Classes.Add(className);
            return this;
        }

        public ProfileNavigationItemBuilder RemoveClass(string className)
        {
            if (_item.Classes.Contains(className))
                _item.Classes.Remove(className);
            return this;
        }

        public ProfileNavigationItemBuilder LinkToFirstChild(bool value)
        {
            _item.LinkToFirstChild = value;
            return this;
        }

        public ProfileNavigationItemBuilder LocalNav()
        {
            _item.LocalNav = true;
            return this;
        }

        public ProfileNavigationItemBuilder Local(bool value)
        {
            _item.LocalNav = value;
            return this;
        }

        public ProfileNavigationItemBuilder Permission(Permission permission)
        {
            _item.Permissions.Add(permission);
            return this;
        }

        public ProfileNavigationItemBuilder Resource(object resource)
        {
            _item.Resource = resource;
            return this;
        }

        public override List<ProfileMenuItem> Build()
        {
            _item.Items = base.Build();
            return new List<ProfileMenuItem> { _item };
        }

        public ProfileNavigationItemBuilder Action(RouteValueDictionary values)
        {
            return values != null
                       ? Action(values["action"] as string, values["controller"] as string, values)
                       : Action(null, null, new RouteValueDictionary());
        }

        public ProfileNavigationItemBuilder Action(string actionName)
        {
            return Action(actionName, null, new RouteValueDictionary());
        }

        public ProfileNavigationItemBuilder Action(string actionName, string controllerName)
        {
            return Action(actionName, controllerName, new RouteValueDictionary());
        }

        public ProfileNavigationItemBuilder Action(string actionName, string controllerName, object values)
        {
            return Action(actionName, controllerName, new RouteValueDictionary(values));
        }

        public ProfileNavigationItemBuilder Action(string actionName, string controllerName, RouteValueDictionary values)
        {
            return Action(actionName, controllerName, null, values);
        }

        public ProfileNavigationItemBuilder Action(string actionName, string controllerName, string areaName)
        {
            return Action(actionName, controllerName, areaName, new RouteValueDictionary());
        }

        public ProfileNavigationItemBuilder Action(string actionName, string controllerName, string areaName, RouteValueDictionary values)
        {
            _item.RouteValues = new RouteValueDictionary(values);
            if (!string.IsNullOrEmpty(actionName))
                _item.RouteValues["action"] = actionName;
            if (!string.IsNullOrEmpty(controllerName))
                _item.RouteValues["controller"] = controllerName;
            if (!string.IsNullOrEmpty(areaName))
                _item.RouteValues["area"] = areaName;
            return this;
        }
    }
}
