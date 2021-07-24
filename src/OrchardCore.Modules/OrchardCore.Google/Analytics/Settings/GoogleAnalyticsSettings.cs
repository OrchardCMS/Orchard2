using System;

namespace OrchardCore.Google.Analytics.Settings
{
    public class GoogleAnalyticsSettings
    {
        public SettingEntry[] SettingEntries { get; set; } = Array.Empty<SettingEntry>();
    }

    public class SettingEntry
    {
        public string MeasurementId { get; set; }

        public bool SendPageView { get; set; } = true;

        public bool IsDefault { get; set; }
    }
}
