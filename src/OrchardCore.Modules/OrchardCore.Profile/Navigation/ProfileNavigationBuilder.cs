using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrchardCore.Profile.Navigation
{
    public class ProfileNavigationBuilder
    {
        List<ProfileMenuItem> Contained { get; set; }

        public ProfileNavigationBuilder()
        {
            Contained = new List<ProfileMenuItem>();
        }

        public ProfileNavigationBuilder Add(LocalizedString caption, string position, Action<ProfileNavigationItemBuilder> itemBuilder, IEnumerable<string> classes = null)
        {
            var childBuilder = new ProfileNavigationItemBuilder();

            childBuilder.Caption(caption);
            childBuilder.Position(position);
            itemBuilder(childBuilder);
            Contained.AddRange(childBuilder.Build());

            if (classes != null)
            {
                foreach (var className in classes)
                    childBuilder.AddClass(className);
            }

            return this;
        }

        public ProfileNavigationBuilder Add(LocalizedString caption, Action<ProfileNavigationItemBuilder> itemBuilder, IEnumerable<string> classes = null)
        {
            return Add(caption, null, itemBuilder, classes);
        }
        public ProfileNavigationBuilder Add(Action<ProfileNavigationItemBuilder> itemBuilder, IEnumerable<string> classes = null)
        {
            return Add(new LocalizedString(null, null), null, itemBuilder, classes);
        }
        public ProfileNavigationBuilder Add(LocalizedString caption, string position, IEnumerable<string> classes = null)
        {
            return Add(caption, position, x => { }, classes);
        }
        public ProfileNavigationBuilder Add(LocalizedString caption, IEnumerable<string> classes = null)
        {
            return Add(caption, null, x => { }, classes);
        }

        public ProfileNavigationBuilder Remove(ProfileMenuItem item)
        {
            Contained.Remove(item);
            return this;
        }

        public ProfileNavigationBuilder Remove(Predicate<ProfileMenuItem> match)
        {
            RemoveRecursive(Contained, match);
            return this;
        }

        public virtual List<ProfileMenuItem> Build()
        {
            return (Contained ?? new List<ProfileMenuItem>()).ToList();
        }

        private static void RemoveRecursive(List<ProfileMenuItem> menuItems, Predicate<ProfileMenuItem> match)
        {
            menuItems.RemoveAll(match);
            foreach (var menuItem in menuItems)
            {
                if (menuItem.Items?.Count > 0)
                {
                    RemoveRecursive(menuItem.Items, match);
                }
            }
        }
    }
}
