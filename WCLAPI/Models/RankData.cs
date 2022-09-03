namespace WCLAPI.Models
{
    public class RankData
    {
        public Encounter? Encounter { get; set; }
        public Role Roles { get; set; } = new Role();
    }
}
