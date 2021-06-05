using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPR.Models
{
    public class OverwatchLeague
    {
        public List<OwlGame> Games { get; set; }
        public List<Standings> Tournaments { get; set; }
    }
}