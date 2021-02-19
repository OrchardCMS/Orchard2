using System.Threading.Tasks;

namespace OrchardCore.Secrets
{
    public interface IDecryptionProvider
    {
        Task<IDecryptor> CreateAsync(string encryptionKey);
    }
}
