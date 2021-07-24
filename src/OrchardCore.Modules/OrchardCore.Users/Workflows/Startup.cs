using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Users.Handlers;
using OrchardCore.Users.Workflows.Activities;
using OrchardCore.Users.Workflows.Drivers;
using OrchardCore.Users.Workflows.Handlers;
using OrchardCore.Workflows.Helpers;

namespace OrchardCore.Users.Workflows
{
    [RequireFeatures("OrchardCore.Workflows")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddActivity<RegisterUserTask, RegisterUserTaskDisplayDriver>();
            services.AddActivity<UserCreatedEvent, UserCreatedEventDisplayDriver>();
            services.AddActivity<UserDeletedEvent, UserDeletedEventDisplayDriver>();
            services.AddActivity<UserEnabledEvent, UserEnabledEventDisplayDriver>();
            services.AddActivity<UserDisabledEvent, UserDisabledEventDisplayDriver>();
            services.AddActivity<UserUpdatedEvent, UserUpdatedEventDisplayDriver>();
            services.AddActivity<UserLoggedInEvent, UserLoggedInEventDisplayDriver>();
            services.AddScoped<IUserEventHandler, UserEventHandler>();
            services.AddActivity<AssignUserRoleTask, AssignUserRoleTaskDisplayDriver>();
            services.AddActivity<ValidateUserTask, ValidateUserTaskDisplayDriver>();
        }
    }

    [RequireFeatures("OrchardCore.Workflows", "OrchardCore.Users.ResetPassword")]
    public class ResetPasswordStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddActivity<ResetUserPasswordTask, ResetUserPasswordTaskDisplay>();
        }
    }
}
