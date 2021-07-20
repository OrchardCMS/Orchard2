using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace OrchardCore.Profile
{
    public interface IProfileService
    {
        /// <summary>
        /// Return the site settings for the current tenant.
        /// </summary>
        Task<IProfile> GetProfileAsync();

        /// <summary>
        /// Persists the changes to the site settings.
        /// </summary>
        Task UpdateProfileAsync(IProfile site);

        /// <summary>
        /// Gets a change token that is set when site settings have changed.
        /// </summary>
        IChangeToken ChangeToken { get; }
    }
}
