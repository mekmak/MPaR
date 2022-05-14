using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.Owl
{
    public class Schedule
    {
        [JsonProperty("data")]
        public Content Content { get; set; }

        public static Schedule FromJson(string response)
        {
            return JsonConvert.DeserializeObject<Schedule>(response);
        }
    }

    public class Content
    {
        [JsonProperty("tableData")]
        public Week Week { get; set; }
    }

    public class Week
    {
        [JsonProperty("name")]
        public string WeekName { get; set; }

        [JsonProperty("weekNumber")]
        public int WeekNumber { get;set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime EndDate {get;set;}

        [JsonProperty("events")]
        public List<Event> Events {get;set;}
    }

    public class Event
    {
        [JsonProperty("matches")]
        public List<Match> Matches {get;set;}
    }

    public class Match
    {
        [JsonProperty("id")]
        public string Id {get;set;}

        [JsonProperty("status")]
        public string Status {get;set;}

        [JsonProperty("scores")]
        public List<int> Scores {get;set;}

        [JsonProperty("startDate")]
        public long? StartDateUnix {get;set;}

        [JsonProperty("endDate")]
        public long? EndDateUnix {get;set;}

        [JsonProperty("encoreDate")]
        public long? EncoreDateUnix { get; set; }

        [JsonProperty("isEncore")]
        public bool IsEncore { get; set; }

        [JsonProperty("competitors")]
        public List<Competitor> Competitors {get;set;}
    }

    public class Competitor
    {
        [JsonProperty("id")]
        public  string Id {get;set;}

        [JsonProperty("name")]
        public string Name {get;set;}

        [JsonProperty("abbreviatedName")]
        public string AbbreviatedName {get;set;}
    }
}