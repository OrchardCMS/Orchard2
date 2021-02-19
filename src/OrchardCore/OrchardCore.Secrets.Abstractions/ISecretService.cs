using System.Threading.Tasks;

namespace OrchardCore.Secrets
{
    public interface ISecretService<TSecret> where TSecret : Secret, new()
    {
          Task<TSecret> GetSecretAsync(string key);
    }

    public class SecretService<TSecret> : ISecretService<TSecret> where TSecret : Secret, new()
    {
        private readonly ISecretCoordinator _secretCoordinator;

        public SecretService(ISecretCoordinator secretCoordinator)
        {
            _secretCoordinator = secretCoordinator;
        }

        public Task<TSecret> GetSecretAsync(string key) => _secretCoordinator.GetSecretAsync<TSecret>(key);
    }
}
