namespace WCLAnalysis.Models
{
    public class Player
    {
        public int SourceID { get; set; }
        public string? Name { get; set; }
        public List<int> EncounterRanking { get; set; } = new List<int>();

        public int WCLAvgRanking
        {
            get { return (int)EncounterRanking.Average(); }
        }
    }
}
