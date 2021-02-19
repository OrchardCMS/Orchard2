using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrchardCore.Workflows.Http.ViewModels
{
    public class HttpRequestEventViewModel
    {
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public bool ValidateAntiforgeryToken { get; set; }

        public int TokenLifeSpan { get; set; }

        public string HttpRequestEventSecret { get; set; }

        public IList<SelectListItem> GetAvailableHttpMethods()
        {
            var availableHttpMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
            return availableHttpMethods.Select(x => new SelectListItem { Text = x, Value = x }).ToList();
        }
    }
}
