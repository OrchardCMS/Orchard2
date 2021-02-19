using System;
using System.Collections.Generic;
using OrchardCore.Data.Documents;

namespace OrchardCore.Secrets.Models
{
    public class SecretsDocument : Document
    {
        public Dictionary<string, DocumentSecret> Secrets { get; } = new Dictionary<string, DocumentSecret>(StringComparer.OrdinalIgnoreCase);
    }

    public class DocumentSecret
    {
        public string Value { get; set; }
    }
}
