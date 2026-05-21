using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.WorldCup
{
    public class FifaCalendar
    {
        [JsonProperty("Results")]
        public List<Match> Results { get; set; }

        public static FifaCalendar FromJson(string response)
        {
            return JsonConvert.DeserializeObject<FifaCalendar>(response);
        }
    }

    public class Match
    {
        [JsonProperty("IdCompetition")]
        public string IdCompetition { get; set; }

        [JsonProperty("IdSeason")]
        public string IdSeason { get; set; }

        [JsonProperty("IdStage")]
        public string IdStage { get; set; }

        [JsonProperty("IdMatch")]
        public string IdMatch { get; set; }

        [JsonProperty("Date")]
        public DateTime DateUtc { get; set; }

        [JsonProperty("LocalDate")]
        public DateTime LocalDate { get; set; }

        [JsonProperty("StageName")]
        public List<LocalizedString> StageName { get; set; }

        [JsonProperty("GroupName")]
        public List<LocalizedString> GroupName { get; set; }

        [JsonProperty("Home")]
        public Team Home { get; set; }

        [JsonProperty("Away")]
        public Team Away { get; set; }

        [JsonProperty("HomeTeamScore")]
        public int? HomeTeamScore { get; set; }

        [JsonProperty("AwayTeamScore")]
        public int? AwayTeamScore { get; set; }

        [JsonProperty("MatchStatus")]
        public int MatchStatus { get; set; }

        [JsonProperty("PlaceHolderA")]
        public string PlaceHolderA { get; set; }

        [JsonProperty("PlaceHolderB")]
        public string PlaceHolderB { get; set; }

        [JsonProperty("Stadium")]
        public Stadium Stadium { get; set; }
    }

    public class Team
    {
        [JsonProperty("IdTeam")]
        public string IdTeam { get; set; }

        [JsonProperty("IdCountry")]
        public string IdCountry { get; set; }

        [JsonProperty("Abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("TeamName")]
        public List<LocalizedString> TeamName { get; set; }
    }

    public class Stadium
    {
        [JsonProperty("Name")]
        public List<LocalizedString> Name { get; set; }

        [JsonProperty("CityName")]
        public List<LocalizedString> CityName { get; set; }

        [JsonProperty("IdCountry")]
        public string IdCountry { get; set; }
    }

    public class LocalizedString
    {
        [JsonProperty("Locale")]
        public string Locale { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }
    }
}
