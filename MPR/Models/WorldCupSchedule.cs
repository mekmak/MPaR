using System;
using System.Collections.Generic;

namespace MPR.Models
{
    public class WorldCupSchedule
    {
        public List<MatchDay> Days { get; set; }
    }

    public class MatchDay
    {
        public DateTime Date { get; set; }
        public int Order { get; set; }
        public List<WorldCupMatch> Matches { get; set; }
    }

    public class WorldCupMatch
    {
        public string GroupFull { get; set; }
        public string GroupShort { get; set; }
        public string LocationFull { get; set; }
        public string LocationShort { get; set; }
        public string HomeFull { get; set; }
        public string HomeShort { get; set; }
        public string HomeLink { get; set; }
        public string AwayFull { get; set; }
        public string AwayShort { get; set; }
        public string AwayLink { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public DateTime StartDate { get; set; }
        public string MatchLink { get; set; }
        public string Status { get; set; }
    }
}
