using System.Collections.Generic;

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

    public class F1Driver
    {
        public string Name {get;set;}
        public int Position {get;set;}
        public int Points {get;set;}
        public string Nationality {get;set;}
        public string Car {get;set;}
    }
}
