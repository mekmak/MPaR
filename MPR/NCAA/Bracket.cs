using Newtonsoft.Json;
using System.Collections.Generic;

namespace MPR.NCAA
{
    public class Bracket
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        public static Bracket FromJson(string response)
        {
            return JsonConvert.DeserializeObject<Bracket>(response);
        }
    }

    public class Data
    {
        [JsonProperty("mmlContests")]
        public List<Game> Games { get; set; }

        [JsonProperty("mmlTournament")]
        public List<Tournament> Tournaments { get; set; }
    }

    public class Tournament
    {
        [JsonProperty("rounds")]
        public List<Round> Rounds { get; set; }
    }

    public class Game
    {
        [JsonProperty("startTimeEpoch")]
        public long StartTimeEpoch { get; set; }

        [JsonProperty("gameState")]
        public string GameState { get; set; }

        [JsonProperty("round")]
        public Round Round { get; set; }        

        [JsonProperty("teams")]
        public List<Team> Teams { get; set; }

        [JsonProperty("currentPeriod")]
        public string Period { get; set; }

        [JsonProperty("contestClock")]
        public string Clock { get; set; }
    }

    public class Round
    {
        [JsonProperty("roundNumber")]
        public int Number { get; set; }

        [JsonProperty("label")]
        public string Name { get; set; }
    }

    public class Team
    {
        [JsonProperty("nameShort")]
        public string Name { get; set; }

        [JsonProperty("score")]
        public int? Score { get; set; }

        [JsonProperty("seed")]
        public string Seed { get; set; }
    }
}