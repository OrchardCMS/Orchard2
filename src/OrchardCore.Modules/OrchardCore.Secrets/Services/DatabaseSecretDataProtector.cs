using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace OrchardCore.Secrets.Services
{
    public class DatabaseSecretDataProtector
    {
        private readonly IDataProtector _dataProtector;
        private readonly ILogger _logger;

        public DatabaseSecretDataProtector(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<DatabaseSecretDataProtector> logger)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(nameof(DatabaseSecretDataProtector));
            _logger = logger;
        }

        public string Protect(string plainText) => _dataProtector.Protect(plainText);

        public string Unprotect(string protectedData)
        {
            try
            {
                return _dataProtector.Unprotect(protectedData);
            }
            catch
            {
                _logger.LogError("The secret could not be decrypted. It may have been encrypted using a different key.");
            }

            return null;
        }
    }
}
