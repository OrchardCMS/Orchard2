using System.Threading.Tasks;

namespace OrchardCore.Environment.Shell.Events
{
    public interface IShellEventHandler
    {
        Task Removing(ShellSettings shellSettings);
    }
}
