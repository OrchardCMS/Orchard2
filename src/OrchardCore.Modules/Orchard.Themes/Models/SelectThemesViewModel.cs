﻿using System.Collections.Generic;
namespace Orchard.Themes.Models
{
    public class SelectThemesViewModel
    {
        public ThemeEntry CurrentTheme { get; set; }
        //public ThemeEntry CurrentAdminTheme { get; set; }
        public IEnumerable<ThemeEntry> Themes { get; set; }
        public string Name { get; set; }
    }
}
