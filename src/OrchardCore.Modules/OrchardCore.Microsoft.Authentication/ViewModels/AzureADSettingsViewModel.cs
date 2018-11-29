using System.ComponentModel.DataAnnotations;

namespace OrchardCore.Microsoft.Authentication.ViewModels
{
    public class AzureADSettingsViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Application Id is required")]
        public string AppId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Tenant Id is required")]
        public string TenantId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Application Secret is required")]
        public string AppSecret { get; set; }

        [RegularExpression(@"\/[-A-Za-z0-9+&@#\/%?=~_|!:,.;]+[-A-Za-z0-9+&@#\/%=~_|]", ErrorMessage = "Invalid path")]
        public string CallbackPath { get; set; }

        [Url]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Instance is required")]
        public string Instance { get; set; }

    }
}
