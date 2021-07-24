using System;
using System.Collections.Generic;

namespace OrchardCore.Environment.Extensions.Features
{
    public class FeatureOptions
    {
        public List<FeatureOption> Rules { get; set; } = new List<FeatureOption>();
    }

    public class FeatureOption
    {
        public string Rule { get; set; }
        public string Expression { get; set; }
    }
}
