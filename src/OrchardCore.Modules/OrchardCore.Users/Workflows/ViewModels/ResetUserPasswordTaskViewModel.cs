using System.ComponentModel.DataAnnotations;

namespace OrchardCore.Users.Workflows.ViewModels
{
    public class ResetUserPasswordTaskViewModel
    {
        [Required]
        public string UserName { get; set; }

        public string ResetPasswordEmailSubject { get; set; }

        [Required]
        public string ResetPasswordEmailTemplate { get; set; }
    }
}
