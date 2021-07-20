using Microsoft.AspNetCore.Routing;
using OrchardCore.Entities;

namespace OrchardCore.Profile
{
    public interface IProfile : IEntity
    {
        string UserName { get; set; }
    }
}
