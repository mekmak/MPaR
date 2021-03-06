﻿namespace MPR.Models.Games
{
    public class OwlGame
    {
        public string HomeTeam { get; set; }
        public string HomeTeamLink { get; set; }
        public string AwayTeam { get; set; }
        public string AwayTeamLink { get; set; }

        public bool AwayTeamWon { get; set; }
        public bool HomeTeamWon { get; set; }

        public string HomeTeamScore { get; set; }
        public string AwayTeamScore { get; set; }

        public string Time { get; set; }
        public string TimeLink { get; set; }

        public string LiveLink { get; set; }

        public int WeekNumber { get; set; }
        public string WeekName { get; set; }
    }
}