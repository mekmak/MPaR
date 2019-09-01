using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPR.Models.Games
{
    public class OwlGame
    {
        public string HomeTeam { get; set; }
        public string HomeTeamLink { get; set; }
        public string AwayTeam { get; set; }
        public string AwayTeamLink { get; set; }

        public string HomeTeamScore { get; set; }
        public string AwayTeamScore { get; set; }

        public bool NotifyAway { get; set; }
        public bool NotifyHome { get; set; }

        public string Score { get; set; }

        public string Time { get; set; }
        public string TimeLink { get; set; }

        public string LiveLink { get; set; }

        public bool IsOver => Score != null && Score.ToLower().Contains("final");

        public string GetSummary()
        {
            return $"Home: {HomeTeam} Score: {HomeTeamScore} Notify: {NotifyHome} Away: {AwayTeam} Score: {AwayTeamScore} Notify: {NotifyAway} Score: {Score} TimeLink: {TimeLink} Is Over: {IsOver}";
        }
    }
}