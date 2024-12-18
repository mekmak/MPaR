﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.F1
{
    public class F1ScoreBoard
    {
        [JsonProperty("sports")]
        public List<Sport> Sports { get; set; }

        public static F1ScoreBoard FromJson(string response)
        {
            return JsonConvert.DeserializeObject<F1ScoreBoard>(response);
        }
    }

    public class Sport
    {
        [JsonProperty("leagues")]
        public List<League> Leagues { get; set; }
    }

    public class League
    {
        [JsonProperty("events")]
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        [JsonProperty("shortName")]
        public string Name { get; set; }

        [JsonProperty("date")]
        public DateTime DateUtc { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("competitionType")]
        public CompetitionType Type { get; set; }

        [JsonProperty("competitors")]
        public List<Competitor> Competitors { get; set; }
    }

    public class Competitor
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("place")]
        public int Place { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("vehicle")]
        public Vehicle Vehicle { get; set; }
    }

    public class Vehicle
    {
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
    }

    public class CompetitionType
    {
        [JsonProperty("text")]
        public string Name { get; set; }

        [JsonProperty("abbreviation")]
        public string ShortName { get; set; }
    }

}