using System.Collections.Generic;
using System.Diagnostics;

namespace MPR.F1
{
    public class F1DriverStandings
    {
        public F1DriverStandings()
        {
            Drivers = new List<F1Driver>();
        }

        public List<F1Driver> Drivers {get;set;}
    }

    [DebuggerDisplay("{FullName}")]
    public class F1Driver
    {
        public string Name {get;set;}
        public string FullName {get;set;}
        public int Position {get;set;}
        public int Points {get;set;}
        public string Nationality {get;set;}
        public string Car {get;set;}
    }

    public class F1RealDriverStandings
    {
        public F1RealDriverStandings()
        {
            Drivers = new List<RealF1Driver>();
        }

        public List<RealF1Driver> Drivers {get;set;}
    }

    [DebuggerDisplay("{FullName}")]
    public class RealF1Driver
    {
        public string Name {get;set;}
        public int Position {get;set;}
        public int FakePosition {get;set;}
        public double Points {get;set;}
        public int FakePoints {get;set;}
        public string Nationality {get;set;}
        public string Car {get;set;}
        public int PositionsMoved {get;set;}
        public double PointsDiff {get;set;}
    }
}
