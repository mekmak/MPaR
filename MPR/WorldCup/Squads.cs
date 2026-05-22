using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.WorldCup
{
    public class Squad
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("abbr")]
        public string Abbr { get; set; }

        [JsonProperty("seed")]
        public int Seed { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("groupPlayed")]
        public int GroupPlayed { get; set; }

        [JsonProperty("groupPosition")]
        public int GroupPosition { get; set; }

        [JsonProperty("groupGoalsDifference")]
        public int GroupGoalsDifference { get; set; }

        [JsonProperty("groupPoints")]
        public int GroupPoints { get; set; }

        [JsonProperty("worldRank")]
        public int WorldRank { get; set; }

        public static List<Squad> FromJson(string response)
        {
            return JsonConvert.DeserializeObject<List<Squad>>(response);
        }
    }
}
