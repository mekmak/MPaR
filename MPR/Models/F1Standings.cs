using System.Collections.Generic;

namespace MPR.Models
{
    public class F1Standings
    {
        public List<F1Team> TeamStandings {get;set;}
        public List<RealF1Team> RealTeamStandings {get;set;}
        public List<F1Driver> DriverStandings {get;set;}
        public List<RealF1Driver> RealDriverStandings {get;set;}
    }

    public class F1Team
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public int Points { get; set; }
    }

    public class RealF1Team
    {
        public string Name { get; set; }
        public int Position {get;set;}
        public int FakePosition { get; set; }
        public int PositionsMoved { get; set; }
        public double Points { get; set; }
        public double PointsDiff { get; set; }
        public int FakePoints { get; set; }
    }

    public class F1Driver
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public int Points { get; set; }
        public string Nationality { get; set; }
        public string Car { get; set; }
    }

    public class RealF1Driver
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public int FakePosition { get; set; }
        public int PositionsMoved { get; set; }
        public double Points { get; set; }
        public int FakePoints { get; set; }
        public double PointsDiff { get; set; }
        public string Nationality { get; set; }
        public string Car { get; set; }
    }
}

