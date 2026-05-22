using System.Collections.Generic;

namespace MPR.Models
{
    public class WorldCupStandings
    {
        public List<WorldCupGroup> Groups { get; set; }
    }

    public class WorldCupGroup
    {
        public string Letter { get; set; }
        public string Header { get; set; }
        public List<WorldCupStandingsRow> Rows { get; set; }
    }

    public class WorldCupStandingsRow
    {
        public int Position { get; set; }
        public string Name { get; set; }
        public string Abbr { get; set; }
        public int WorldRank { get; set; }
        public int Played { get; set; }
        public int GoalDifference { get; set; }
        public int Points { get; set; }
        public string TeamLink { get; set; }
    }
}
