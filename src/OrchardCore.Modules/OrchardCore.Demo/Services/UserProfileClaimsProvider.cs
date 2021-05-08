using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using OrchardCore.Demo.Models;
using OrchardCore.Entities;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;

namespace OrchardCore.Demo.Services
{
    internal class UserProfileClaimsProvider : IUserClaimsProvider
    {
        public Task GenerateAsync(IUser user, ClaimsIdentity claims)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            var u = user as User;
            var profile = u.As<UserProfile>();

            claims.AddClaim(new Claim("preferred_username", user.UserName));

            var name = "";
            if (!string.IsNullOrEmpty(profile.FirstName))
            {
                claims.AddClaim(new Claim("given_name", profile.FirstName));
                name += profile.FirstName;
            }

            if (!string.IsNullOrEmpty(profile.LastName))
            {
                claims.AddClaim(new Claim("family_name", profile.LastName));
                name += $" {profile.LastName}";
            }

            if (!string.IsNullOrEmpty(name))
            {
                claims.AddClaim(new Claim("name", name));
            }

            if (profile.UpdatedAt != default)
            {
                claims.AddClaim(new Claim("updated_at", ConvertToUnixTimestamp(profile.UpdatedAt).ToString(CultureInfo.InvariantCulture)));
            }

            return Task.FromResult(claims);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
    }
}
