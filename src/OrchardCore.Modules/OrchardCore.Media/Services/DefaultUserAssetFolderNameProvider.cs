using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OrchardCore.Media.Services
{
    public class DefaultUserAssetFolderNameProvider: IUserAssetFolderNameProvider
    {
        public string GetUserAssetFolderName(ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
