using System.Threading.Tasks;

namespace OrchardCore.Profile.Navigation
{
    public interface IProfileNavigationProvider
    {
        Task BuildNavigation(string name, ProfileNavigationBuilder builder);
    }
}
