using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Services.Description;
using MPR.Models.Games;
using MPR.Rest;
using WebGrease;

namespace MPR.ScoreConnectors
{
    public class EspnScoreConnector
    {
        private readonly List<string> _nameSubs = new List<string>
        {
            "Flaccid Chodes",
            "Suckville",
            "Butt Munchers",
            "Grundle Grabbers",
            "Shoplifters",
            "Beliebers",
            "Murderers",
            "Wife Beaters"
        };

        private static readonly Dictionary<Sport, List<Game>> GameCache = new Dictionary<Sport, List<Game>>();
        private static readonly Dictionary<string, Tuple<string, string>> ScoreCache = new Dictionary<string, Tuple<string, string>>();

        private static readonly object GameCacheLocker = new object();
        private static readonly object PullInitLocker = new object();

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
                Name = "Game Pull",
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
                    List<Game> games = DownloadGames(sport);
                    lock (GameCacheLocker)
                    {
                        GameCache[sport] = games;
                    }
                }

                Thread.Sleep(10000);
            }
        }

        public void ResetCache()
        {
            GameCache.Clear();
            ScoreCache.Clear();
        }

        public List<Game> GetGames(Sport sport)
        {
            if (!GameCache.ContainsKey(sport))
            {
                return new List<Game>();
            }

            List<Game> games = GameCache[sport];
            return games;
        }

        private List<Game> DownloadGames(Sport sport)
        {
            var client = new RestClient(GetEndPoint(sport));
            string requestRespone = client.MakeRequest();

            //Logger.Info(string.Format("Request response: {0}", requestRespone));

            NameValueCollection keyValues = HttpUtility.ParseQueryString(requestRespone);

            int gameCount = GetCount(sport, keyValues);
            var games = new List<Game>();
            for (int gameNumber = 1; gameNumber <= gameCount; gameNumber++)
            {
                string score = GetScore(sport, keyValues, gameNumber);
                string link = GetLink(sport, keyValues, gameNumber);
                GameInfo gameInfo = GetGameInfo(sport, keyValues, gameNumber);

                var game = new Game
                {
                    HomeTeam = gameInfo.HomeTeam,
                    HomeTeamScore = gameInfo.HomeTeamScore,
                    AwayTeam = gameInfo.AwayTeam,
                    AwayTeamScore = gameInfo.AwayTeamScore,
                    Score = score,
                    Link = link,
                    Time = gameInfo.Time
                };

                SetShouldNotify(sport, game);
                UpdateNames(game);

                //Logger.Info(string.Format("Game: {0}", game));
                games.Add(game);
            }

            return games;
        }

        private void UpdateNames(Game game)
        {
            if (game.AwayTeam.Equals("Baltimore"))
            {
                game.AwayTeam = GetNameSub();
            }

            if (game.HomeTeam.Equals("Baltimore"))
            {
                game.HomeTeam = GetNameSub();
            }
        }

        private string GetNameSub()
        {
            int count = _nameSubs.Count;
            var rand = new Random();
            int num = rand.Next(0, count - 1);
            return _nameSubs[num];
        }

        private void SetShouldNotify(Sport sport, Game game)
        {
            string gameKey = $"{sport}.{game.HomeTeam}.{game.AwayTeam}";

            if (ScoreCache.ContainsKey(gameKey))
            {
                if (!game.HomeTeamScore.Equals(ScoreCache[gameKey].Item1))
                {
                    game.NotifyHome = true;
                }

                if (!game.AwayTeamScore.Equals(ScoreCache[gameKey].Item2))
                {
                    game.NotifyAway = true;
                }
            }


            ScoreCache[gameKey] = new Tuple<string, string>(game.HomeTeamScore, game.AwayTeamScore);
        }

        private int GetCount(Sport sport, NameValueCollection collection)
        {
            return Int32.Parse(collection[GetCountKey(sport)]);
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
                string[] splits = score.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                string time = "";
                try
                {
                    time = splits[splits.Length - 1];
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

                foreach (var split in splits)
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