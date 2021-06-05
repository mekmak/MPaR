using System.Collections.Generic;

namespace MPR.Models
{
    public class Standings
    {
        public string TournamentName { get; set; }
        public List<Region> Regions { get; set; }
    }

    public class Region
    {
        public string Name { get; set; }
        public List<Team> Teams { get; set; }
    }

    public class Team
    {
        public string Name { get; set; }
        public int Rank { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public string MapDiff { get; set; }
        public int Points { get; set; }
        public string WinLoss { get; set; }
        public bool MakesCutoff { get; set; }
    }
}