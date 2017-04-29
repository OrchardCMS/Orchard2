﻿using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Orchard.Localization.Core
{
    public class PoStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ILocalizationManager _localizationManager;

        public PoStringLocalizerFactory(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return new PoStringLocalizer(CultureInfo.CurrentUICulture, null, _localizationManager);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return new PoStringLocalizer(CultureInfo.CurrentUICulture, null, _localizationManager);
        }
    }
}
