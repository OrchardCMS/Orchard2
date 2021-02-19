namespace OrchardCore.Secrets
{
    public interface IEncryptor
    {
        string Encrypt(string plainText);
    }
}
