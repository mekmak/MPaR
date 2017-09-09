using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Web;
using MPR.Models.Games;
using MPR.Rest;
using WebGrease;

namespace MPR.ScoreConnectors
{
    public class EspnScoreConnector
    {
        private List<string> NameSubs = new List<string>
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

        private static Dictionary<Sport, List<Game>> GameCache = new Dictionary<Sport, List<Game>>();
        private static Dictionary<string, Tuple<string, string>> ScoreCache = new Dictionary<string, Tuple<string, string>>();

        private static readonly object GameCacheLocker = new object();
        private static readonly object ScoreCacheLocker = new object();

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
            return string.Format(@"http://sports.espn.go.com/{0}/bottomline/scores", sport);
        }

        private string GetCountKey(Sport sport)
        {
            return string.Format("{0}_s_count", sport);
        }

        private string GetGameKey(Sport sport)
        {
            return string.Format("{0}_s_left", sport);
        }

        private string GetUrlKey(Sport sport)
        {
            return string.Format("{0}_s_url", sport);
        }

        #endregion

        private static EspnScoreConnector _instance;
        public static EspnScoreConnector Instance
        {
            get { return _instance ?? (_instance = new EspnScoreConnector()); }
        }

        private EspnScoreConnector()
        {
            Thread thread = new Thread(new ThreadStart(UpdateGames));
            thread.Name = "Game Pull";
            thread.Priority = ThreadPriority.Normal;
            thread.IsBackground = true;
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
                Tuple<string, string, string, string> teams = GetTeamsAndScores(sport, keyValues, gameNumber);

                var game = new Game
                {
                    HomeTeam = teams.Item1,
                    HomeTeamScore = teams.Item2,
                    AwayTeam = teams.Item3,
                    AwayTeamScore = teams.Item4,
                    Score = score,
                    Link = link
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
            int count = NameSubs.Count;
            var rand = new Random();
            int num = rand.Next(0, count - 1);
            return NameSubs[num];
        }

        private void SetShouldNotify(Sport sport, Game game)
        {
            string gameKey = string.Format("{0}.{1}.{2}", sport, game.HomeTeam, game.AwayTeam);

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
            return collection[string.Format("{0}{1}", GetGameKey(sport), gameNumber)];
        }

        private Tuple<string, string, string, string> GetTeamsAndScores(Sport sport, NameValueCollection collection, int gameNumber)
        {
            string score = GetScore(sport, collection, gameNumber);

            if (score.IndexOf(" at ") < 0)
            {
                string[] splits = score.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
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

                return new Tuple<string, string, string, string>(homeTeam, homeTeamScore, awayTeam, awayTeamScore);
            }
            else
            {
                string[] splits = score.Split(new string[] { " at " }, StringSplitOptions.RemoveEmptyEntries);
                string homeTeam = splits[1].Split(new string[] { "(" }, StringSplitOptions.RemoveEmptyEntries)[0];
                string awayTeam = splits[0];
                return new Tuple<string, string, string, string>(homeTeam, "-", awayTeam, "-");
            }
        }

        private string GetLink(Sport sport, NameValueCollection collection, int gameNumber)
        {
            return collection[string.Format("{0}{1}", GetUrlKey(sport), gameNumber)];
        }
    }
}