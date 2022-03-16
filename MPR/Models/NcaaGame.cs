using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPR.Models
{
    public class NcaaBracket
    {
        public List<NcaaRound> Rounds { get; set; }
        public List<NcaaGame> Games { get; set; }
    }

    public class NcaaGame
    {
        public int RoundNumber { get; set; }

        public string State { get; set; }
        public DateTime StartTime { get; set; }
        public string Period { get; set; }
        public string Clock { get; set; }

        public NcaaTeam TeamOne { get; set; }
        public NcaaTeam TeamTwo { get; set; }
    }

    public class NcaaRound
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class NcaaTeam
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public string Seed { get; set; }
        public bool Winner { get; set; }
    }

    
}