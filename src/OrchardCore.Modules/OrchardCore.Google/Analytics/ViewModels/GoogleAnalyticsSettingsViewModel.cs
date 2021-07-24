using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.Google.Analytics.Settings;

namespace OrchardCore.Google.Analytics.ViewModels
{
    public class GoogleAnalyticsSettingsViewModel
    {
        [BindNever]
        public SettingEntry[] SettingEntries { get; set; } = Array.Empty<SettingEntry>();

        public string DefaultMeasurementId { get; set; }

        public string Settings { get; set; }

    }
}
