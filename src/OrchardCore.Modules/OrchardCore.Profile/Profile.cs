using OrchardCore.Entities;

namespace OrchardCore.Profile
{
    public class Profile : Entity, IProfile
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }
}
