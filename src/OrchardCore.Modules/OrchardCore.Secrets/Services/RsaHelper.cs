using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace OrchardCore.Secrets.Services
{
    public static class RsaHelper
    {
        public static RSA GenerateRsaSecurityKey(int size)
        {
            // By default, the default RSA implementation used by .NET Core relies on the newest Windows CNG APIs.
            // Unfortunately, when a new key is generated using the default RSA.Create() method, it is not bound
            // to the machine account, which may cause security exceptions when running Orchard on IIS using a
            // virtual application pool identity or without the profile loading feature enabled (off by default).
            // To ensure a RSA key can be generated flawlessly, it is manually created using the managed CNG APIs.
            // For more information, visit https://github.com/openiddict/openiddict-core/issues/204.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Warning: ensure a null key name is specified to ensure the RSA key is not persisted by CNG.
                var key = CngKey.Create(CngAlgorithm.Rsa, keyName: null, new CngKeyCreationParameters
                {
                    ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey,
                    Parameters = { new CngProperty("Length", BitConverter.GetBytes(size), CngPropertyOptions.None) }
                });

                return new RSACng(key);
            }

            return RSA.Create(size);
        }        
    }
}
