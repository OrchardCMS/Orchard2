using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Commands;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Liquid;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Recipes.Services;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;
using OrchardCore.Setup.Events;
using OrchardCore.Users.Commands;
using OrchardCore.Users.Controllers;
using OrchardCore.Users.Drivers;
using OrchardCore.Users.Handlers;
using OrchardCore.Users.Indexes;
using OrchardCore.Users.Liquid;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using OrchardCore.Users.ViewModels;
using YesSql.Filters.Query;
using YesSql.Indexes;

namespace OrchardCore.Users
{
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;
        private readonly string _tenantName;

        public Startup(IOptions<AdminOptions> adminOptions, ShellSettings shellSettings)
        {
            _adminOptions = adminOptions.Value;
            _tenantName = shellSettings.Name;
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var userOptions = serviceProvider.GetRequiredService<IOptions<UserOptions>>().Value;

            var accountControllerName = typeof(AccountController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "Login",
                areaName: "OrchardCore.Users",
                pattern: userOptions.LoginPath,
                defaults: new { controller = accountControllerName, action = nameof(AccountController.Login) }
            );
            routes.MapAreaControllerRoute(
                name: "ChangePassword",
                areaName: "OrchardCore.Users",
                pattern: userOptions.ChangePasswordUrl,
                defaults: new { controller = accountControllerName, action = nameof(AccountController.ChangePassword) }
            );
            routes.MapAreaControllerRoute(
                name: "UsersLogOff",
                areaName: "OrchardCore.Users",
                pattern: userOptions.LogoffPath,
                defaults: new { controller = accountControllerName, action = nameof(AccountController.LogOff) }
            );

            routes.MapAreaControllerRoute(
                name: "ExternalLogins",
                areaName: "OrchardCore.Users",
                pattern: userOptions.ExternalLoginsUrl,
                defaults: new { controller = accountControllerName, action = nameof(AccountController.ExternalLogins) }
            );

            var adminControllerName = typeof(AdminController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "UsersIndex",
                areaName: "OrchardCore.Users",
                pattern: _adminOptions.AdminUrlPrefix + "/Users/Index",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Index) }
            );
            routes.MapAreaControllerRoute(
                name: "UsersCreate",
                areaName: "OrchardCore.Users",
                pattern: _adminOptions.AdminUrlPrefix + "/Users/Create",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Create) }
            );
            routes.MapAreaControllerRoute(
                name: "UsersDelete",
                areaName: "OrchardCore.Users",
                pattern: _adminOptions.AdminUrlPrefix + "/Users/Delete/{id}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Delete) }
            );
            routes.MapAreaControllerRoute(
                name: "UsersEdit",
                areaName: "OrchardCore.Users",
                pattern: _adminOptions.AdminUrlPrefix + "/Users/Edit/{id?}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Edit) }
            );
            routes.MapAreaControllerRoute(
                name: "UsersEditPassword",
                areaName: "OrchardCore.Users",
                pattern: _adminOptions.AdminUrlPrefix + "/Users/EditPassword/{id}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.EditPassword) }
            );
            routes.MapAreaControllerRoute(
                name: "UsersUnlock",
                areaName: "OrchardCore.Users",
                pattern: _adminOptions.AdminUrlPrefix + "/Users/Unlock/{id}",
                defaults: new { controller = adminControllerName, action = nameof(AdminController.Unlock) }
            );

            builder.UseAuthorization();
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<UserOptions>(userOptions =>
            {
                var configuration = ShellScope.Services.GetRequiredService<IShellConfiguration>();
                configuration.GetSection("OrchardCore_Users").Bind(userOptions);
            });

            // Add ILookupNormalizer as Singleton because it is needed by UserIndexProvider
            services.TryAddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();

            // Adds the default token providers used to generate tokens for reset passwords, change email
            // and change telephone number operations, and for two factor authentication token generation.
            services.AddIdentity<IUser, IRole>(options =>
            {
                // Specify OrchardCore User requirements.
                // A user name cannot include an @ symbol, i.e. be an email address
                // An email address must be provided, and be unique.
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._+";
                options.User.RequireUniqueEmail = true;
            })
            .AddDefaultTokenProviders();

            // Configure the authentication options to use the application cookie scheme as the default sign-out handler.
            // This is required for security modules like the OpenID module (that uses SignOutAsync()) to work correctly.
            services.AddAuthentication(options => options.DefaultSignOutScheme = IdentityConstants.ApplicationScheme);

            services.TryAddScoped<UserStore>();
            services.TryAddScoped<IUserStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserRoleStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserPasswordStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserEmailStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserSecurityStampStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserLoginStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserClaimStore<IUser>>(sp => sp.GetRequiredService<UserStore>());
            services.TryAddScoped<IUserAuthenticationTokenStore<IUser>>(sp => sp.GetRequiredService<UserStore>());

            services.ConfigureApplicationCookie(options =>
            {
                var userOptions = ShellScope.Services.GetRequiredService<IOptions<UserOptions>>();

                options.Cookie.Name = "orchauth_" + HttpUtility.UrlEncode(_tenantName);

                // Don't set the cookie builder 'Path' so that it uses the 'IAuthenticationFeature' value
                // set by the pipeline and comming from the request 'PathBase' which already ends with the
                // tenant prefix but may also start by a path related e.g to a virtual folder.

                options.LoginPath = "/" + userOptions.Value.LoginPath;
                options.LogoutPath = "/" + userOptions.Value.LogoffPath;
                options.AccessDeniedPath = "/Error/403";
            });

            services.AddSingleton<IIndexProvider, UserIndexProvider>();
            services.AddSingleton<IIndexProvider, UserByRoleNameIndexProvider>();
            services.AddSingleton<IIndexProvider, UserByLoginInfoIndexProvider>();
            services.AddSingleton<IIndexProvider, UserByClaimIndexProvider>();
            services.AddScoped<IDataMigration, Migrations>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserClaimsPrincipalFactory<IUser>, DefaultUserClaimsPrincipalProviderFactory>();
            services.AddScoped<IUserClaimsProvider, EmailClaimsProvider>();
            services.AddSingleton<IUserIdGenerator, DefaultUserIdGenerator>();

            services.AddScoped<IAuthorizationHandler, UserAuthorizationHandler>();

            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<ISetupEventHandler, SetupEventHandler>();
            services.AddScoped<ICommandHandler, UserCommands>();
            services.AddScoped<IRoleRemovedEventHandler, UserRoleRemovedEventHandler>();
            services.AddScoped<IExternalLoginEventHandler, ScriptExternalLoginEventHandler>();

            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            services.AddScoped<IDisplayDriver<ISite>, LoginSettingsDisplayDriver>();

            services.AddScoped<IDisplayManager<User>, DisplayManager<User>>();
            services.AddScoped<IDisplayDriver<User>, UserDisplayDriver>();
            services.AddScoped<IDisplayDriver<User>, UserRoleDisplayDriver>();
            services.AddScoped<IDisplayDriver<User>, UserInformationDisplayDriver>();
            services.AddScoped<IDisplayDriver<User>, UserButtonsDisplayDriver>();

            services.AddScoped<IThemeSelector, UsersThemeSelector>();

            services.AddScoped<IRecipeEnvironmentProvider, RecipeEnvironmentSuperUserProvider>();

            services.AddScoped<IUsersAdminListQueryService, DefaultUsersAdminListQueryService>();

            services.AddScoped<IDisplayManager<UserIndexOptions>, DisplayManager<UserIndexOptions>>();
            services.AddScoped<IDisplayDriver<UserIndexOptions>, UserOptionsDisplayDriver>();

            services.AddSingleton<IUsersAdminListFilterParser>(sp =>
            {
                var filterProviders = sp.GetServices<IUsersAdminListFilterProvider>();
                var builder = new QueryEngineBuilder<User>();
                foreach (var provider in filterProviders)
                {
                    provider.Build(builder);
                }

                var parser = builder.Build();

                return new DefaultUsersAdminListFilterParser(parser);
            });

            services.AddTransient<IUsersAdminListFilterProvider, DefaultUsersAdminListFilterProvider>();

        }
    }

    [RequireFeatures("OrchardCore.Liquid")]
    public class LiquidStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<TemplateOptions>(o =>
            {
                o.Filters.AddFilter("has_claim", UserFilters.HasClaim);
                o.Filters.AddFilter("user_id", UserFilters.UserId);

                o.MemberAccessStrategy.Register<ClaimsPrincipal>();
                o.MemberAccessStrategy.Register<ClaimsIdentity>();

                o.Scope.SetValue("User", new ObjectValue(new LiquidUserAccessor()));
                o.MemberAccessStrategy.Register<LiquidUserAccessor, FluidValue>((obj, name, ctx) =>
                {
                    var user = ((LiquidTemplateContext)ctx).Services.GetRequiredService<IHttpContextAccessor>().HttpContext?.User;
                    if (user != null)
                    {
                        return name switch
                        {
                            nameof(ClaimsPrincipal.Identity) => new ObjectValue(user.Identity),
                            _ => NilValue.Instance
                        };
                    }

                    return NilValue.Instance;
                });

                o.MemberAccessStrategy.Register<User, FluidValue>((user, name, context) =>
                {
                    return name switch
                    {
                        nameof(User.UserId) => new StringValue(user.UserId),
                        nameof(User.UserName) => new StringValue(user.UserName),
                        nameof(User.NormalizedUserName) => new StringValue(user.NormalizedUserName),
                        nameof(User.Email) => new StringValue(user.Email),
                        nameof(User.NormalizedEmail) => new StringValue(user.NormalizedEmail),
                        nameof(User.EmailConfirmed) => user.EmailConfirmed ? BooleanValue.True : BooleanValue.False,
                        nameof(User.IsEnabled) => user.IsEnabled ? BooleanValue.True : BooleanValue.False,
                        nameof(User.RoleNames) => new ArrayValue(user.RoleNames.Select(x => new StringValue(x))),
                        nameof(User.Properties) => new ObjectValue(user.Properties),
                        _ => NilValue.Instance
                    };
                });
            })
           .AddLiquidFilter<UsersByIdFilter>("users_by_id")
           .AddLiquidFilter<HasPermissionFilter>("has_permission")
           .AddLiquidFilter<IsInRoleFilter>("is_in_role")
           .AddLiquidFilter<UserEmailFilter>("user_email");
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class LoginDeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<LoginSettings, LoginDeploymentStartup>(S => S["Login settings"], S => S["Exports the Login settings."]);
        }
    }

    [Feature("OrchardCore.Users.ChangeEmail")]
    public class ChangeEmailStartup : StartupBase
    {
        private const string ChangeEmailPath = "ChangeEmail";
        private const string ChangeEmailConfirmationPath = "ChangeEmailConfirmation";

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "ChangeEmail",
                areaName: "OrchardCore.Users",
                pattern: ChangeEmailPath,
                defaults: new { controller = "ChangeEmail", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "ChangeEmailConfirmation",
                areaName: "OrchardCore.Users",
                pattern: ChangeEmailConfirmationPath,
                defaults: new { controller = "ChangeEmail", action = "ChangeEmailConfirmation" }
            );
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<TemplateOptions>(o =>
            {
                o.MemberAccessStrategy.Register<ChangeEmailViewModel>();
            });

            services.AddScoped<INavigationProvider, ChangeEmailAdminMenu>();
            services.AddScoped<IDisplayDriver<ISite>, ChangeEmailSettingsDisplayDriver>();
        }
    }

    [Feature("OrchardCore.Users.ChangeEmail")]
    [RequireFeatures("OrchardCore.Deployment")]
    public class ChangeEmailDeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<ChangeEmailDeploymentStartup, ChangeEmailDeploymentStartup>(S => S["Change Email settings"], S => S["Exports the Change Email settings."]);
        }
    }

    [Feature("OrchardCore.Users.Registration")]
    public class RegistrationStartup : StartupBase
    {
        private const string RegisterPath = nameof(RegistrationController.Register);
        private const string ConfirmEmailSent = nameof(RegistrationController.ConfirmEmailSent);
        private const string RegistrationPending = nameof(RegistrationController.RegistrationPending);

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: RegisterPath,
                areaName: "OrchardCore.Users",
                pattern: RegisterPath,
                defaults: new { controller = "Registration", action = RegisterPath }
            );

            routes.MapAreaControllerRoute(
                name: ConfirmEmailSent,
                areaName: "OrchardCore.Users",
                pattern: ConfirmEmailSent,
                defaults: new { controller = "Registration", action = ConfirmEmailSent }
            );

            routes.MapAreaControllerRoute(
                name: RegistrationPending,
                areaName: "OrchardCore.Users",
                pattern: RegistrationPending,
                defaults: new { controller = "Registration", action = RegistrationPending }
            );
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<TemplateOptions>(o =>
            {
                o.MemberAccessStrategy.Register<ConfirmEmailViewModel>();
            });

            services.AddScoped<INavigationProvider, RegistrationAdminMenu>();
            services.AddScoped<IDisplayDriver<ISite>, RegistrationSettingsDisplayDriver>();
        }
    }

    [Feature("OrchardCore.Users.Registration")]
    [RequireFeatures("OrchardCore.Deployment")]
    public class RegistrationDeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<RegistrationSettings, RegistrationDeploymentStartup>(S => S["Registration settings"], S => S["Exports the Registration settings."]);
        }
    }

    [Feature("OrchardCore.Users.ResetPassword")]
    public class ResetPasswordStartup : StartupBase
    {
        private const string ForgotPasswordPath = "ForgotPassword";
        private const string ForgotPasswordConfirmationPath = "ForgotPasswordConfirmation";
        private const string ResetPasswordPath = "ResetPassword";
        private const string ResetPasswordConfirmationPath = "ResetPasswordConfirmation";

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "ForgotPassword",
                areaName: "OrchardCore.Users",
                pattern: ForgotPasswordPath,
                defaults: new { controller = "ResetPassword", action = "ForgotPassword" }
            );
            routes.MapAreaControllerRoute(
                name: "ForgotPasswordConfirmation",
                areaName: "OrchardCore.Users",
                pattern: ForgotPasswordConfirmationPath,
                defaults: new { controller = "ResetPassword", action = "ForgotPasswordConfirmation" }
            );
            routes.MapAreaControllerRoute(
                name: "ResetPassword",
                areaName: "OrchardCore.Users",
                pattern: ResetPasswordPath,
                defaults: new { controller = "ResetPassword", action = "ResetPassword" }
            );
            routes.MapAreaControllerRoute(
                name: "ResetPasswordConfirmation",
                areaName: "OrchardCore.Users",
                pattern: ResetPasswordConfirmationPath,
                defaults: new { controller = "ResetPassword", action = "ResetPasswordConfirmation" }
            );
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<TemplateOptions>(o =>
            {
                o.MemberAccessStrategy.Register<LostPasswordViewModel>();
            });

            services.AddScoped<INavigationProvider, ResetPasswordAdminMenu>();
            services.AddScoped<IDisplayDriver<ISite>, ResetPasswordSettingsDisplayDriver>();
        }
    }

    [Feature("OrchardCore.Users.ResetPassword")]
    [RequireFeatures("OrchardCore.Deployment")]
    public class ResetPasswordDeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSiteSettingsPropertyDeploymentStep<ResetPasswordDeploymentStartup, ResetPasswordDeploymentStartup>(S => S["Reset Password settings"], S => S["Exports the Reset Password settings."]);
        }
    }

    [Feature("OrchardCore.Users.CustomUserSettings")]
    public class CustomUserSettingsStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDisplayDriver<User>, CustomUserSettingsDisplayDriver>();
            services.AddScoped<IPermissionProvider, CustomUserSettingsPermissions>();
        }
    }
}
