using Microsoft.Extensions.Localization;
using OrchardCore.AuditTrail.Providers;
using OrchardCore.AuditTrail.Services.Models;
using OrchardCore.Modules;

namespace OrchardCore.Users.AuditTrail.Providers
{
    [RequireFeatures("OrchardCore.AuditTrail")]
    public class UserAuditTrailEventProvider : AuditTrailEventProviderBase
    {
        public const string Registered = nameof(Registered);
        public const string LoggedIn = nameof(LoggedIn);
        public const string LogInFailed = nameof(LogInFailed);
        public const string LogInIsLockedOut = nameof(LogInIsLockedOut);
        public const string Enabled = nameof(Enabled);
        public const string Disabled = nameof(Disabled);
        public const string Created = nameof(Created);
        public const string Updated = nameof(Updated);
        public const string Deleted = nameof(Deleted);

        public UserAuditTrailEventProvider(IStringLocalizer<UserAuditTrailEventProvider> stringLocalizer) : base(stringLocalizer)
        { }

        public override void Describe(DescribeContext context)
        {
            if (context.Category != null && context.Category != "User")
            {
                return;
            }

            context.For("User", S["User"])
                .Event(LoggedIn, S["Logged in"], S["A user was successfully logged in."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(LogInFailed, S["Login failed"], S["An attempt to login failed."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(LogInIsLockedOut, S["Login account locked"], S["An attempt to login failed because the user is locked out."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(Enabled, S["Enabled"], S["A user was enabled."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(Disabled, S["Disabled"], S["A user was disabled."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(Created, S["Created"], S["A user was created."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(Updated, S["Updated"], S["A user was updated."], UserAuditTrailEventProviderHelper.BuildEvent, true)
                .Event(Deleted, S["Deleted"], S["A user was deleted."], UserAuditTrailEventProviderHelper.BuildEvent, true);
        }
    }
}
