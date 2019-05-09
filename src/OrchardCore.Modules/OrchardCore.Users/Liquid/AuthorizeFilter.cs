using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Security.Permissions;
using OrchardCore.Liquid;

namespace OrchardCore.Users.Liquid
{
    public class AuthorizeFilter : ILiquidFilter
    {
        public async ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (!context.AmbientValues.TryGetValue("Services", out var servicesObj))
            {
                throw new ArgumentException("Services missing while invoking 'authorize'");
            }

            var services = servicesObj as IServiceProvider;

            var auth = services.GetRequiredService<IAuthorizationService>();
            var permissionProviders = services.GetRequiredService<IEnumerable<IPermissionProvider>>();

            var clearance = false;
            var permissionName = arguments["permission"].Or(arguments.At(0)).ToStringValue();
            var resource = arguments["resource"].Or(arguments.At(1)).ToObjectValue();

            var permission = permissionProviders.SelectMany(pp => pp.GetPermissions().Where(p => p.Name == permissionName))
                                .Where(c => c != null).FirstOrDefault();

            if (permission is Permission && input.ToObjectValue() is ClaimsPrincipal principal)
            {
                clearance = await auth.AuthorizeAsync(principal, permission,resource);
            }

            return clearance ? BooleanValue.True : BooleanValue.False;
        }
    }
}
