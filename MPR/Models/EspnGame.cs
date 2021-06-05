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
        private int _homeTeamScore => int.TryParse(HomeTeamScore, out var score) ? score : 0;
        public string AwayTeamScore { get; set; }
        private int _awayTeamScore => int.TryParse(AwayTeamScore, out var score) ? score : 0;

        public bool NotifyAway { get; set; }
        public bool AwayTeamWon => IsOver && _awayTeamScore > _homeTeamScore;
        public bool NotifyHome { get; set; }
        public bool HomeTeamWon => IsOver && _homeTeamScore > _awayTeamScore;

        public string Score { get; set; }

        public string Time { get; set; }
        public string TimeLink { get; set; }

        public bool IsOver => Time == "Final";

    }
}