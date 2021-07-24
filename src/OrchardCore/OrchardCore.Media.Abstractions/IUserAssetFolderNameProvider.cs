using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OrchardCore.Media
{
    public interface IUserAssetFolderNameProvider
    {
        string GetUserAssetFolderName(ClaimsPrincipal claimsPrincipal);
    }
}
