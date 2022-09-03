using WCLAPI.Models;

namespace WCLAnalysis.Models
{
    public class Report
    {
        public List<Fight>? Fights { get; set; }
        public MasterData? MasterData { get; set; }
        public Event? Events { get; set; }
        public Table? Table { get; set; }
        public Ranking Rankings { get; set; } = new Ranking();
    }
}
