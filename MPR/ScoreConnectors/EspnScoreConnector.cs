using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Web;
using MPR.Models.Games;
using MPR.Rest;

namespace MPR.ScoreConnectors
{
    public class EspnScoreConnector
    {
        private readonly Dictionary<string, string> _shortNameMap = new Dictionary<string, string>
        {
            {"Kansas City", "KC"},
            {"Oakland", "OAK"},
            {"Philadelphia", "PHI"},
            {"Washington", "WAS"},
            {"New England", "NE"},
            {"Atlanta", "ATL"},
            {"NY Giants", "NYG"},
            {"Seattle", "SEA"},
            {"Los Angeles", "LA"},
            {"Denver", "DEN"},
            {"Pittsburgh", "PIT"},
            {"Cincinnati", "CIN"},
            {"San Francisco", "SF"},
            {"Dallas", "DAL"},
            {"Minnesota", "MIN"},
            {"Baltimore", "BAL"},
            {"Miami", "MIA"},
            {"NY Jets", "NYJ"},
            {"Arizona", "ARI"},
            {"Green Bay", "GB"},
            {"New Orleans", "NO"},
            {"Cleveland", "CLE"},
            {"Tennessee", "TEN"},
            {"Chicago", "CHI"},
            {"Carolina", "CAR"},
            {"Buffalo", "BUF"},
            {"Tampa Bay", "TB"},
            {"Detroit", "DET"},
            {"Houston", "HOU"},
            {"Jacksonville", "JAX" },
            {"Indianapolis", "IND" }
        };

        private readonly ConcurrentDictionary<Sport, List<EspnGame>> _gameCache = new ConcurrentDictionary<Sport, List<EspnGame>>();
        private readonly Dictionary<string, Tuple<string, string>> _scoreCache = new Dictionary<string, Tuple<string, string>>();

        #region Sport

        public enum Sport
        {
            nfl,
            mlb,
            nba,
            nhl
        }

        private string GetEndPoint(Sport sport)
        {
            return $@"http://sports.espn.go.com/{sport}/bottomline/scores";
        }

        private string GetCountKey(Sport sport)
        {
            return $"{sport}_s_count";
        }

        private string GetGameKey(Sport sport)
        {
            return $"{sport}_s_left";
        }

        private string GetUrlKey(Sport sport)
        {
            return $"{sport}_s_url";
        }

        #endregion

        public static EspnScoreConnector Instance = new EspnScoreConnector();

        public void InitGameDownload()
        {
            var thread = new Thread(UpdateGames)
            {
                Name = "Espn EspnGame Pull",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            };
            thread.Start();
        }

        private void UpdateGames()
        {
            while (true)
            {
                foreach (Sport sport in Enum.GetValues(typeof(Sport)).Cast<Sport>())
                {
                    List<EspnGame> games = DownloadGames(sport);
                    _gameCache.AddOrUpdate(sport, _ => games, (v1, v2) => games);
                }

                Thread.Sleep(10000);
            }
        }

        public List<EspnGame> GetGames(Sport sport)
        {
            List<EspnGame> games;
            if(_gameCache.TryGetValue(sport, out games))
            {
                return games;
            }

            return new List<EspnGame>();
        }

        private List<EspnGame> DownloadGames(Sport sport)
        {
            var client = new RestClient(GetEndPoint(sport));
            string requestResponse = client.MakeRequest();

            NameValueCollection keyValues = HttpUtility.ParseQueryString(requestResponse);

            int gameCount = GetCount(sport, keyValues);
            var games = new List<EspnGame>();
            for (int gameNumber = 1; gameNumber <= gameCount; gameNumber++)
            {
                string score = GetScore(sport, keyValues, gameNumber);
                string link = GetLink(sport, keyValues, gameNumber);
                GameInfo gameInfo = GetGameInfo(sport, keyValues, gameNumber);

                string homeTeam = sport == Sport.nfl ? TryShorten(gameInfo.HomeTeam) : gameInfo.HomeTeam;
                string awayTeam = sport == Sport.nfl ? TryShorten(gameInfo.AwayTeam) : gameInfo.AwayTeam;

                var game = new EspnGame
                {
                    HomeTeam = homeTeam,
                    HomeTeamScore = gameInfo.HomeTeamScore,
                    AwayTeam = awayTeam,
                    AwayTeamScore = gameInfo.AwayTeamScore,
                    Score = score,
                    TimeLink = link,
                    Time = gameInfo.Time
                };

                SetShouldNotify(sport, game);
                games.Add(game);
            }

            return games;
        }

        private string TryShorten(string teamName)
        {
            string shortened;
            return !_shortNameMap.TryGetValue(teamName.Trim(), out shortened) ? teamName : shortened;
        }
        
        private void SetShouldNotify(Sport sport, EspnGame espnGame)
        {
            string gameKey = $"{sport}.{espnGame.HomeTeam}.{espnGame.AwayTeam}";

            if (_scoreCache.ContainsKey(gameKey))
            {
                if (!espnGame.HomeTeamScore.Equals(_scoreCache[gameKey].Item1))
                {
                    espnGame.NotifyHome = true;
                }

                if (!espnGame.AwayTeamScore.Equals(_scoreCache[gameKey].Item2))
                {
                    espnGame.NotifyAway = true;
                }
            }


            _scoreCache[gameKey] = new Tuple<string, string>(espnGame.HomeTeamScore, espnGame.AwayTeamScore);
        }

        private int GetCount(Sport sport, NameValueCollection collection)
        {
            return int.Parse(collection[GetCountKey(sport)]);
        }

        private string GetScore(Sport sport, NameValueCollection collection, int gameNumber)
        {
            return collection[$"{GetGameKey(sport)}{gameNumber}"];
        }

        private class GameInfo
        {
            public GameInfo(string homeTeam, string homeTeamScore, string awayTeam, string awayTeamScore, string time)
            {
                HomeTeam = homeTeam;
                HomeTeamScore = homeTeamScore;
                AwayTeam = awayTeam;
                AwayTeamScore = awayTeamScore;
                Time = time;
            }

            public string HomeTeam { get; }
            public string HomeTeamScore { get; }
            public string AwayTeam { get; }
            public string AwayTeamScore { get; }
            public string Time { get; }
        }

        private GameInfo GetGameInfo(Sport sport, NameValueCollection collection, int gameNumber)
        {
            string score = GetScore(sport, collection, gameNumber);

            if (score.IndexOf(" at ", StringComparison.Ordinal) < 0)
            {
                string[] splits = score.Split(new [] { " " }, StringSplitOptions.RemoveEmptyEntries);
                string[] timeSplits = score.Split(new[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);

                string time = "";
                try
                {
                    time = timeSplits[timeSplits.Length - 1];
                    if (time != "FINAL")
                    {
                        var quarterSplits = time.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                        time = quarterSplits[quarterSplits.Length - 1].ToLower();
                    }
                }
                catch
                {
                    // ignored
                }

                string homeTeam = "";
                string homeTeamScore = "";

                bool awayTeamSet = false;
                string awayTeam = "";
                string awayTeamScore = "";

                foreach (string split in splits)
                {
                    int teamScore = 0;
                    if (int.TryParse(split, out teamScore))
                    {
                        if (awayTeamSet)
                        {
                            homeTeamScore = teamScore.ToString();
                            break;
                        }
                        else
                        {
                            awayTeamScore = teamScore.ToString();
                            awayTeamSet = true;
                        }

                    }
                    else
                    {
                        if (!awayTeamSet)
                        {
                            awayTeam = awayTeam + " " + split;
                        }
                        else
                        {
                            homeTeam = homeTeam + " " + split;
                        }
                    }
                }

                homeTeam = homeTeam.Replace("^", "");
                awayTeam = awayTeam.Replace("^", "");

                return new GameInfo(homeTeam, homeTeamScore, awayTeam, awayTeamScore, time);
            }
            else
            {
                string[] splits = score.Split(new [] { " at " }, StringSplitOptions.RemoveEmptyEntries);
                string[] homeSplits = splits[1].Split(new [] {"(", ")"}, StringSplitOptions.RemoveEmptyEntries);
                string homeTeam = homeSplits[0];
                string time = homeSplits[1];
                string awayTeam = splits[0];
                return new GameInfo(homeTeam, "-", awayTeam, "-", time);
            }
        }

        private string GetLink(Sport sport, NameValueCollection collection, int gameNumber)
        {
            return collection[$"{GetUrlKey(sport)}{gameNumber}"];
        }
    }
}