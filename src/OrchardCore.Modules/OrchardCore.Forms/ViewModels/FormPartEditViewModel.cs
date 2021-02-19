namespace OrchardCore.Forms.ViewModels
{
    public class FormPartEditViewModel
    {
        public string Action { get; set; }
        public string Method { get; set; }
        public string WorkflowTypeId { get; set; }
        public string EncType { get; set; }
        public bool EnableAntiForgeryToken { get; set; } = true;
        public string HttpRequestEventSecret { get; set; }
    }
}
