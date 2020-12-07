using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Setup.Events;
using OrchardCore.Users.Models;

namespace OrchardCore.Users.Services
{
    /// <summary>
    /// During setup, creates the admin user account.
    /// </summary>
    public class SetupEventHandler : ISetupEventHandler
    {
        private readonly IUserService _userService;

        public SetupEventHandler(IUserService userService)
        {
            _userService = userService;
        }

        public Task Setup(
            string siteName,
            string userName,
            string userId,
            string email,
            string password,
            string dbProvider,
            string dbConnectionString,
            string dbTablePrefix,
            string siteTimeZone,
            Action<string, string> reportError,
            IDictionary<string, string> properties
            )
        {
            var user = new User
            {
                UserName = userName,
                UserId = userId,
                Email = email,
                RoleNames = new string[] { "Administrator" },
                EmailConfirmed = true
            };

            return _userService.CreateUserAsync(user, password, reportError);
        }
    }
}
