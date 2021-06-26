using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OrchardCore.Search.Elastic.ViewModels
{
    public class QueryIndexViewModel
    {
        public string Query { get; set; }
        public string IndexName { get; set; }

        [BindNever]
        public TimeSpan Duration { get; set; }

        [BindNever]
        public IEnumerable<Dictionary<string,object>> Documents { get; set; } = Enumerable.Empty<Dictionary<string,object>>();
    }
}
