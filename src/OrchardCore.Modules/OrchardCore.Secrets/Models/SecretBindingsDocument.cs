using System;
using System.Collections.Generic;
using OrchardCore.Data.Documents;

namespace OrchardCore.Secrets.Models
{
    public class SecretBindingsDocument : Document
    {
        public IDictionary<string, SecretBinding> SecretBindings { get; } = new Dictionary<string, SecretBinding>(StringComparer.OrdinalIgnoreCase);
    }
}
