using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using OrchardCoreLocalization = OrchardCore.Localization;
using OrchardCore.Settings;
using Xunit;

namespace OrchardCore.Tests.Settings
{
    public class SiteSettingsTests
    {
        private const string InvariantCulture = "";

        private static readonly string _nonSupportedCulture = "it";

        private Mock<ISite> _site;

        public SiteSettingsTests()
        {
            _site = new Mock<ISite>();
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData(InvariantCulture, null, InvariantCulture, null)]
        [InlineData(InvariantCulture, new string[] { InvariantCulture }, InvariantCulture, new string[] { InvariantCulture })]
        [InlineData(null, new string[] { }, null, null)]
        [InlineData(null, new string[] { "ar", "fr" }, null, new string[] { "ar", "fr" })]
        [InlineData("ar", new string[] { "ar", "fr" }, "ar", new string[] { "ar", "fr" })]
        public async Task SiteReturnsConfiguredCultures(string defaultCulture, string[] supportedCultures, string expectedDefaultCulture, string[] expectedSupportedCultures)
        {
            SimulateEnvironmentCulture();

            if (defaultCulture == null)
            {
                expectedDefaultCulture = CultureInfo.CurrentCulture.Name;
            }

            if (supportedCultures == null || supportedCultures.Length == 0)
            {
                expectedSupportedCultures = new[] { CultureInfo.CurrentCulture.Name };
            }

            await RunTestWithAcceptLanguageHttpHeader(_nonSupportedCulture, defaultCulture, supportedCultures, expectedDefaultCulture, expectedSupportedCultures);
            await RunTestWithQueryString(defaultCulture, supportedCultures, expectedDefaultCulture, expectedSupportedCultures);
        }

        [Fact]
        public async Task SiteReturnsConfiguredCulturesWithInvariantCulture()
        {
            await RunTestWithAcceptLanguageHttpHeader(_nonSupportedCulture, CultureInfo.InstalledUICulture.Name, new string[] { }, CultureInfo.InstalledUICulture.Name, new[] { CultureInfo.InstalledUICulture.Name });
            await RunTestWithQueryString(CultureInfo.InstalledUICulture.Name, new string[] { }, CultureInfo.InstalledUICulture.Name, new[] { CultureInfo.InstalledUICulture.Name });
        }

        private async Task RunTestWithQueryString(string defaultCulture, string[] supportedCultures, string expectedDefaultCulture, string[] expectedSupportedCultures)
        {
            var localizationStartup = new OrchardCoreLocalization.Startup();
            var webHostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    SetupSiteSettingsCultures(defaultCulture, supportedCultures);
                    services.AddTransient<ISiteService>(_ => new SiteService(_site.Object));

                    services.AddRouting();

                    localizationStartup.ConfigureServices(services);
                })
                .Configure(app =>
                {
                    localizationStartup.Configure(app, new RouteBuilder(app), app.ApplicationServices);

                    app.Run(context =>
                    {
                        var requestLocalizationOptions = context.RequestServices.GetService<IOptions<RequestLocalizationOptions>>().Value;

                        Assert.Equal(expectedDefaultCulture, requestLocalizationOptions.DefaultRequestCulture.Culture.Name);
                        Assert.Equal(expectedSupportedCultures, requestLocalizationOptions.SupportedCultures.Select(c => c.Name).ToArray());

                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(webHostBuilder))
            {
                var client = server.CreateClient();
                var requestedCulture = "en";
                var response = await client.GetAsync($"page/?culture={requestedCulture}&ui-culture={requestedCulture}");
            }
        }

        private async Task RunTestWithAcceptLanguageHttpHeader(string acceptLanguages, string defaultCulture, string[] supportedCultures, string expectedDefaultCulture, string[] expectedSupportedCultures)
        {
            var localizationStartup = new OrchardCoreLocalization.Startup();
            var webHostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    SetupSiteSettingsCultures(defaultCulture, supportedCultures);
                    services.AddTransient<ISiteService>(_ => new SiteService(_site.Object));

                    services.AddRouting();

                    localizationStartup.ConfigureServices(services);
                })
                .Configure(app =>
                {
                    localizationStartup.Configure(app, new RouteBuilder(app), app.ApplicationServices);

                    app.Run(context =>
                    {
                        var requestLocalizationOptions = context.RequestServices.GetService<IOptions<RequestLocalizationOptions>>().Value;

                        Assert.Equal(expectedDefaultCulture, requestLocalizationOptions.DefaultRequestCulture.Culture.Name);
                        Assert.Equal(expectedSupportedCultures, requestLocalizationOptions.SupportedCultures.Select(c => c.Name).ToArray());

                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(webHostBuilder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguages);

                var response = await client.GetAsync(String.Empty);
            }
        }

        private void SetupSiteSettingsCultures(string defaultCulture, string[] supportedCultures)
        {
            _site.Setup(s => s.Culture).Returns(defaultCulture);
            _site.Setup(s => s.SupportedCultures).Returns(supportedCultures);
        }

        private void SimulateEnvironmentCulture()
        {
            var culturesList = new[] { InvariantCulture, "ar", "fr", "en-US", "it-IT", "fr-CA" };
            var environmentCulture = culturesList[new Random().Next(0, culturesList.Length - 1)];

            SetCurrentThreadCulture(environmentCulture);
        }

        private void SetCurrentThreadCulture(string culture)
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
        }

        private class SiteService : ISiteService
        {
            private readonly ISite _site;

            public SiteService(ISite site)
            {
                _site = site;
            }

            public IChangeToken ChangeToken => throw new NotImplementedException();

            public Task<ISite> GetSiteSettingsAsync() => Task.FromResult(_site);

            public Task UpdateSiteSettingsAsync(ISite site)
            {
                throw new NotImplementedException();
            }
        }
    }
}