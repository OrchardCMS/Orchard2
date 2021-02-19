namespace OrchardCore.Secrets
{
    public interface IDecryptor
    {
        string Decrypt(string protectedData);
    }
}
