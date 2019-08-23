using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.Owl
{
    public class Schedule
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        public static Schedule FromJson(string response)
        {
            return JsonConvert.DeserializeObject<Schedule>(response);
        }
    }

    public class Data
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("stages")] public List<Stage> Stages { get; set; }
    }

    public class Stage
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("weeks")]
        public List<Week> Weeks { get; set; }
    }

    public class Week
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("startDate")]
        public long StartDate { get; set; }

        [JsonProperty("endDate")]
        public long EndDate { get; set; }

        [JsonProperty("matches")]
        public List<Match> Matches { get; set; }
    }

    public class Match
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("startDateTS")]
        public long StartDate { get; set; }

        [JsonProperty("endDateTS")]
        public long EndDate { get; set; }

        [JsonProperty("competitors")]
        public List<Competitor> Competitors { get; set; }

        [JsonProperty("scores")]
        public List<Score> Scores { get; set; }

        [JsonProperty("games")]
        public List<MatchGame> Games { get; set; }
    }

    public class Competitor
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("abbreviatedName")]
        public string AbbreviatedName { get; set; }
    }

    public class Score
    {
        [JsonProperty("value")]
        public int Value { get; set; }
    }

    public class MatchGame
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("points")]
        public List<int> Points { get; set; }
    }
}