using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Deployment
{
    public enum DeploymentSecretHandler
    {
        PlainText,
        Empty,
        Encrypted,
        Ignored
    }

    public class DeploymentStepSecret
    {
        public DeploymentStepSecret(string secretName)
        {
            Handler = $"[js: secretsHandler('{secretName}')]";
            Value = $"[js: secrets('{secretName}')]";
        }
        public string Handler { get; set; }
        public string Value { get; set; }
    }

    public enum RecipeSecretHandler
    {
        PlainText,
        Encrypted
    }

    public class RecipeSecret
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeSecretHandler Handler { get; set; }
        public string Value { get; set; }

    }
}
