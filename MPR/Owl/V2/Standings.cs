using System.Collections.Generic;
using Newtonsoft.Json;

namespace MPR.Owl.V2
{
    public class StandingsResponse
    {
        [JsonProperty("props")]
        public Props Props { get; set; }

        public static StandingsResponse FromJson(string json)
        {
            return JsonConvert.DeserializeObject<StandingsResponse>(json);
        }
    }

    public class Props
    {
        [JsonProperty("pageProps")]
        public PageProps PageProps { get; set; }
    }

    public class PageProps
    {
        [JsonProperty("blocks")]
        public List<Block> Blocks { get; set; }
    }

    public class Block
    {
        [JsonProperty("standings")]
        public Standings Standings { get; set; }
    }

    public class Standings
    {
        [JsonProperty("tabs")]
        public List<Tournament> Tournaments { get; set; }
    }

    public class Tournament
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("tables")]
        public List<Region> Regions { get; set; }
    }

    public class Region
    {
        [JsonProperty("section")]
        public string Name { get; set; }

        [JsonProperty("teams")]
        public List<Team> Teams { get; set; }
    }

    public class Team
    {
        [JsonProperty("teamAbbName")]
        public string Name { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("w")]
        public int Wins { get; set; }

        [JsonProperty("l")]
        public int Loses { get; set; }

        [JsonProperty("diff")]
        public string MapDiff { get; set; }

        [JsonProperty("pts")]
        public int Points { get; set; }

        [JsonProperty("mapwlt")]
        public string WinLoss { get; set; }

        [JsonProperty("mp")]
        public int MapsPlayed { get; set; }
    }
}