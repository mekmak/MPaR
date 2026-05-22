using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.Meat
{
    public class ScoreBoard
    {
        [JsonProperty("sports")]
        public List<Sport> Sports { get; set; }

        public static ScoreBoard FromJson(string response)
        {
            return JsonConvert.DeserializeObject<ScoreBoard>(response);
        }
    }

    public class Sport
    {
        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("leagues")]
        public List<League> Leagues { get; set; }
    }

    public class League
    {
        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("events")]
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("date")]
        public DateTime DateUtc { get; set; }

        [JsonProperty("fullStatus")]
        public FullStatus FullStatus { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }

        [JsonProperty("competitors")]
        public List<Competitor> Competitors { get; set; }
    }

    public class FullStatus
    {
        [JsonProperty("type")]
        public StatusType Type { get; set; }
    }

    public class StatusType
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("shortDetail")]
        public string ShortDetail { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class Link
    {
        [JsonProperty("rel")]
        public List<string> Rel { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class Competitor
    {
        [JsonProperty("homeAway")]
        public string HomeAway { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("score")]
        public string Score { get; set; }
    }
}
