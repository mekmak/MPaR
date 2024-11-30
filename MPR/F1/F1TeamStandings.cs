using System.Collections.Generic;
using System.Diagnostics;

namespace MPR.F1
{
    public class F1TeamStandings
    {
        public F1TeamStandings()
        {
            Teams = new List<F1Team>();
        }

        public List<F1Team> Teams {get;set;}
    }

    [DebuggerDisplay("{Name}")]
    public class F1Team
    {
        public string Name {get;set;}
        public int Position {get;set;}
        public int Points {get;set;}
    }

    public class F1RealTeamStandings
    {
        public F1RealTeamStandings()
        {
            Teams = new List<RealF1Team>();
        }

        public List<RealF1Team> Teams {get;set;}
    }

    [DebuggerDisplay("{Name}")]
    public class RealF1Team
    {
        public string Name { get; set; }
        public int Position {get;set;}
        public int FakePosition { get; set; }
        public int PositionsMoved { get; set; }
        public double PointsDiff { get; set; }
        public double Points { get; set; }
        public int FakePoints { get; set; }
    }
}
