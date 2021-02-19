using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Secrets.KeyVault.Services;
using OrchardCore.Secrets.KeyVault.Models;

namespace OrchardCore.Secrets.KeyVault
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<KeyVaultClientService>();
            services.AddScoped<ISecretStore, KeyVaultSecretStore>();

            services.AddTransient<IConfigureOptions<SecretsKeyVaultOptions>, KeyVaultOptionsConfiguration>();
        }
    }
}
