using System.Collections.Generic;

namespace MPR.Models
{
    public class MeatSports
    {
        public List<MeatSportLeague> Leagues { get; set; }
    }

    public class MeatSportLeague
    {
        public string Name { get; set; }
        public List<MeatSportGame> Games { get; set; }
    }
}
