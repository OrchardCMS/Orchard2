using System.Threading.Tasks;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Events;

namespace OrchardCore.Media.Events
{
    public class DeleteMediaShellEventHandler : IShellEventHandler
    {
        private readonly IMediaFileStore _mediaFileStore;

        public DeleteMediaShellEventHandler(IMediaFileStore mediaFileStore)
        {
            _mediaFileStore = mediaFileStore;
        }

        public async Task Removing(ShellSettings shellSettings)
        {
            var success = await _mediaFileStore.TryDeleteDirectoryAsync("/");
        }
    }
}
