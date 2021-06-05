using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPR.Models
{
    public class MeatSports
    {
        public List<MeatSport> Sports { get; set; }
    }

    public class MeatSport
    {
        public string Name { get; set; }
        public List<EspnGame> Games { get; set; }
    }
}