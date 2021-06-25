using System.Collections.Generic;

namespace OrchardCore.Localization
{
    public interface ILocalizationDataProvider
    {
        IEnumerable<DataLocalizedString> GetDescriptors();
    }
}
