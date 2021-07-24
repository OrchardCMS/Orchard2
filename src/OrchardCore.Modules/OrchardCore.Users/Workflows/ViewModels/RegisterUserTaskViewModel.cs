namespace OrchardCore.Users.Workflows.ViewModels
{
    public class RegisterUserTaskViewModel
    {
        public bool SendConfirmationEmail { get; set; }

        public string ConfirmationEmailSubject { get; set; }

        public string ConfirmationEmailTemplate { get; set; }

        public bool RequireModeration { get; set; }
    }
}
