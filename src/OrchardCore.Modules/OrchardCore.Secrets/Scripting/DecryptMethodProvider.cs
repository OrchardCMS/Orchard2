using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Secrets;
using OrchardCore.Scripting;

namespace OrchardCore.Secrets.Scripting
{
    public class DecryptMethodProvider : IGlobalMethodProvider
    {
        private readonly GlobalMethod _globalMethod;

        public DecryptMethodProvider()
        {
            _globalMethod = new GlobalMethod
            {
                Name = "decrypt",
                Method = serviceProvider => (Func<string, object>)(protectedData =>
                {
                    var decryptionProvider = serviceProvider.GetRequiredService<IDecryptionProvider>();
                    var decryptor = decryptionProvider.CreateAsync(protectedData).GetAwaiter().GetResult();
                    return decryptor.Decrypt(protectedData);
                })
            };
        }

        public IEnumerable<GlobalMethod> GetMethods()
        {
            yield return _globalMethod;
        }
    }
}
