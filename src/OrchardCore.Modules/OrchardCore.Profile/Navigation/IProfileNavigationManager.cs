using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace OrchardCore.Profile.Navigation
{
    public interface IProfileNavigationManager
    {
        IEnumerable<ProfileMenuItem> BuildMenu(string name, ActionContext context);
    }
}
