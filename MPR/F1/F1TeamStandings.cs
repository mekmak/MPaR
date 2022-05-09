using System.Collections.Generic;

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

    public class F1Team
    {
        public string Name {get;set;}
        public int Position {get;set;}
        public int Points {get;set;}
    }
}
