using System.Threading.Tasks;

namespace WCLAPI.Models
{
    public class Role
    {
        public Tanks Tanks { get; set; }=new Tanks();
        public Healers Healers { get; set; } = new Healers();
        public Dps Dps { get; set; } = new Dps();
    }
}
public class Tanks
{
    public string Name { get; set; } = "";
    public List<Character> Characters { get; set; } = new List<Character>();
}
public class Dps
{
    public string Name { get; set; } = "";
    public List<Character> Characters { get; set; } = new List<Character>();
}

public class Healers
{
    public string Name { get; set; } = "";
    public List<Character> Characters { get; set; } = new List<Character>();
}

public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int RankPercent { get; set; }
}