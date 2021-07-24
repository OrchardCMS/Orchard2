using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;
using OrchardCore.Environment.Shell;
using OrchardCore.ResourceManagement;
using OrchardCore.Resources;
using OrchardCore.Settings;
using Xunit;

namespace OrchardCore.Tests.Modules.OrchardCore.Resources
{
    public class SubResourceIntegrityTests
    {
        private const string WebRoot = "wwwroot";

        private string RootPath => new DirectoryInfo("../../../../../").FullName;

        [Fact]
        public void CheckSubResourceIntegrity()
        {
            // Arrange
            var orchardCoreResourcesPath = Path.Combine(RootPath, @"src\OrchardCore.Modules\OrchardCore.Resources");
            var siteServiceMock = new Mock<ISiteService>();
            siteServiceMock
                .Setup(s => s.GetSiteSettingsAsync())
                .Returns(Task.FromResult<ISite>(new SiteSettings { ResourceDebugMode = ResourceDebugMode.Enabled }));
            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock
                .Setup(e => e.ContentRootFileProvider)
                .Returns(new PhysicalFileProvider(Path.Combine(orchardCoreResourcesPath, WebRoot)));
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock
                .Setup(a => a.HttpContext)
                .Returns(new DefaultHttpContext());
            var shellSettings = new ShellSettings();
            var configurationOptions = new ResourceManagementOptionsConfiguration(
                siteServiceMock.Object,
                hostEnvironmentMock.Object,
                httpContextAccessorMock.Object,
                shellSettings
            );
            var resourceManagementOptions = new ResourceManagementOptions();

            // Act & Assert
            configurationOptions.Configure(resourceManagementOptions);

            var resourceManifest = resourceManagementOptions.ResourceManifests.First();

            ValidateSubResourceIntegrity("script");
            ValidateSubResourceIntegrity("style");

            void ValidateSubResourceIntegrity(string resourceType)
            {
                var orchardCoreResourceFolder = "~/OrchardCore.Resources";
                var contentRootProvider = hostEnvironmentMock.Object.ContentRootFileProvider;
                foreach (var resource in resourceManifest.GetResources(resourceType))
                {
                    foreach (var resourceDefinition in resource.Value)
                    {
                        if (!String.IsNullOrEmpty(resourceDefinition.CdnIntegrity) && !String.IsNullOrEmpty(resourceDefinition.Url))
                        {
                            var resourcePath = contentRootProvider.GetFileInfo(resourceDefinition.Url[orchardCoreResourceFolder.Length..]).PhysicalPath;
                            var resourceIntegrity = GetSubResourceIntegrity(resourcePath);
                            Assert.Equal(resourceIntegrity, resourceDefinition.CdnIntegrity);
                        }

                        if (!String.IsNullOrEmpty(resourceDefinition.CdnDebugIntegrity) && !String.IsNullOrEmpty(resourceDefinition.UrlDebug))
                        {
                            var resourceDebugPath = contentRootProvider.GetFileInfo(resourceDefinition.UrlDebug[orchardCoreResourceFolder.Length..]).PhysicalPath;
                            var resourceDebugintegrity = GetSubResourceIntegrity(resourceDebugPath);
                            Assert.Equal(resourceDebugintegrity, resourceDefinition.CdnDebugIntegrity);
                        }
                    }
                }
            }
        }

        private static string GetSubResourceIntegrity(string resourcePath)
        {
            var resourceIntegrity = String.Empty;
            var resourceBytes = File.ReadAllBytes(resourcePath);
            using (var sha384 = SHA384.Create())
            {
                var hash = sha384.ComputeHash(resourceBytes);
                resourceIntegrity = "sha384-" + Convert.ToBase64String(hash);
            }

            return resourceIntegrity;
        }
    }
}
