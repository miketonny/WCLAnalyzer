namespace WCLAnalysis.Models
{
    public class PlayerStats
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int UtilPotUsed { get; set; }
        public int ManaPotUsed { get; set; }
        public int ScrollUsed { get; set; }
        public int WCL { get; set; }
        public int Food { get; set; }
        public int Encounters { get; set; }
    }
}
