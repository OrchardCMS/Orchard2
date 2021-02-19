namespace OrchardCore.Secrets
{
    public interface ISecretFactory
    {
        string Name { get; }
        Secret Create();
    }

    public class SecretFactory<TSecret> : ISecretFactory where TSecret : Secret, new()
    {
        private static readonly string TypeName = typeof(TSecret).Name;

        public string Name => TypeName;

        public Secret Create()
        {
            return new TSecret();
        }
    }
}
