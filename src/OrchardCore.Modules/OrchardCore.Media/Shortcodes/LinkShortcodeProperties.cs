namespace OrchardCore.Media.Shortcodes
{
    public class LinkShortcodeProperties : ShortcodePropertiesBase

    {
        private LinkShortcodeProperties(string name) : this(name, name)
        {
        }

        private LinkShortcodeProperties(string name, string mapping)
        {
            Name = name;
            Mapping = mapping;
        }

        public static LinkShortcodeProperties Url
        {
            get
            {
                return new LinkShortcodeProperties("url", "href");
            }
        }
        public static LinkShortcodeProperties Save
        {
            get
            {
                return new LinkShortcodeProperties("save", "download");
            }
        }
        public static LinkShortcodeProperties Tooltip
        {
            get
            {
                return new LinkShortcodeProperties("tooltip", "title");
            }
        }
        public static LinkShortcodeProperties Class
        {
            get
            {
                return new LinkShortcodeProperties("class");
            }
        }
    }
}
