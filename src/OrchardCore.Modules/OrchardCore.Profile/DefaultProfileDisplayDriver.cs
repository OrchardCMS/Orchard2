using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Environment.Shell;
using OrchardCore.Profile.ViewModels;
using YesSql;

namespace OrchardCore.Profile.Drivers
{
    public class DefaultProfileDisplayDriver : DisplayDriver<IProfile>
    {
        public const string GroupId = "general";
        private readonly INotifier _notifier;
        private readonly IShellHost _shellHost;
        private readonly ShellSettings _shellSettings;
        private readonly ISession _session;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;

        public DefaultProfileDisplayDriver(
            INotifier notifier,
            IShellHost shellHost,
            ShellSettings shellSettings,
            ISession session,
            IContentItemDisplayManager contentItemDisplayManager,
            IHtmlLocalizer<DefaultProfileDisplayDriver> h)
        {
            _notifier = notifier;
            _shellHost = shellHost;
            _session = session;
            _shellSettings = shellSettings;
            _contentItemDisplayManager = contentItemDisplayManager;
            H = h;
        }

        IHtmlLocalizer H { get; set; }


        public override IDisplayResult Edit(IProfile profile, IUpdateModel updater)
        {
            return null;
        }
    }
}
