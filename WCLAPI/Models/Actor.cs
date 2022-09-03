namespace WCLAnalysis.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? PetOwner { get; set; } // ownder's id if the actor's a pet..
        public string SubType { get; set; } = "";
    }
}