﻿using System;
using Orchard.Environment.Extensions;

namespace Orchard.Modules
{
    public static class ManifestExtensions
    {
        public static bool IsModule(this IManifestInfo manifestInfo)
        {
            return manifestInfo.Type.Equals("module", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTheme(this IManifestInfo manifestInfo)
        {
            return manifestInfo.Type.Equals("theme", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCore(this IManifestInfo manifestInfo)
        {
            return string.IsNullOrEmpty(manifestInfo.Type);
        }
    }
}
