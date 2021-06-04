using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPR.Models
{
    public class EspnGame
    {
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }

        public string HomeTeamScore { get; set; }
        public string AwayTeamScore { get; set; }

        public bool NotifyAway { get; set; }
        public bool NotifyHome { get; set; }

        public string Score { get; set; }

        public string Time { get; set; }
        public string TimeLink { get; set; }
    }
}