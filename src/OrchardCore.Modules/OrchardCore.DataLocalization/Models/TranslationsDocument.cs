using System;
using System.Collections.Generic;
using OrchardCore.Data.Documents;

namespace OrchardCore.DataLocalization.Models
{
    public class TranslationsDocument : Document
    {
        public Dictionary<string, Translation> Translations { get; set; } = new Dictionary<string, Translation>(StringComparer.OrdinalIgnoreCase);
    }
}
