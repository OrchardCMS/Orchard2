namespace OrchardCore.Search.Elastic.ViewModels
{
    public class ElasticIndexDeploymentStepViewModel
    {
        public bool IncludeAll { get; set; }
        public string[] IndexNames { get; set; }
        public string[] AllIndexNames { get; set; }
    }
}
