using System.ComponentModel;

namespace OrchardCore.ContentFields.Settings
{
    public class ContentPickerFieldSettings
    {
        public string Hint { get; set; }
        public bool Required { get; set; }
        public bool Multiple { get; set; }
        public bool DisplayAllContentTypes { get; set; }
        public string[] DisplayedContentTypes { get; set; } = new string[0];

        /// <summary>
        /// The pattern used to build custom title
        /// </summary>
        public string TitlePattern { get; set; }

        /// <summary>
        /// The pattern used to build additional description
        /// </summary>
        public string DescriptionPattern { get; set; }
    }
}
