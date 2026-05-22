using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MPR.Meat;
using MPR.Models;

namespace MPR.Connectors
{
    public class MeatSportsConnector : Connector
    {
        public static MeatSportsConnector Instance = new MeatSportsConnector();

        private const string ApiUrl =
            "https://site.web.api.espn.com/apis/personalized/v2/scoreboard/header";

        public enum Tab
        {
            Basketball,
            Football,
            Hockey,
            Baseball,
            Soccer
        }

        private static readonly Dictionary<Tab, List<string>> TabLeagues =
            new Dictionary<Tab, List<string>>
        {
            { Tab.Basketball, new List<string> { "NBA", "WNBA" } },
            { Tab.Football,   new List<string> { "NFL", "NCAAF" } },
            { Tab.Hockey,     new List<string> { "NHL" } },
            { Tab.Baseball,   new List<string> { "MLB" } },
            { Tab.Soccer,     new List<string> { "MLS" } },
        };

        private static readonly HashSet<string> AllowedLeagues =
            new HashSet<string>(TabLeagues.Values.SelectMany(v => v));

        private readonly ConcurrentDictionary<string, List<MeatSportGame>> _gamesByLeague =
            new ConcurrentDictionary<string, List<MeatSportGame>>();
        private readonly Dictionary<string, Tuple<string, string>> _scoreCache =
            new Dictionary<string, Tuple<string, string>>();

        public void Init(CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var pulls = new[]
            {
                new Pull { Name = "Meat Sports", Task = UpdateGames }
            };

            StartPulls(token, pulls);
        }

        public MeatSports GetGames(Tab tab)
        {
            var leagues = TabLeagues[tab]
                .Select(abbr => new MeatSportLeague
                {
                    Name = abbr,
                    Games = _gamesByLeague.TryGetValue(abbr, out var games)
                        ? games
                        : new List<MeatSportGame>()
                })
                .ToList();

            return new MeatSports { Leagues = leagues };
        }

        private async Task UpdateGames(CancellationToken token)
        {
            var board = await FetchScoreBoard(token);
            if (board?.Sports == null)
            {
                return;
            }

            var parsed = new Dictionary<string, List<MeatSportGame>>();
            foreach (var sport in board.Sports)
            {
                foreach (var league in sport.Leagues ?? new List<Meat.League>())
                {
                    if (string.IsNullOrEmpty(league.Abbreviation) ||
                        !AllowedLeagues.Contains(league.Abbreviation))
                    {
                        continue;
                    }

                    var games = (league.Events ?? new List<Meat.Event>())
                        .Select(e => ToGame(league.Abbreviation, e))
                        .Where(g => g != null)
                        .ToList();

                    foreach (var g in games)
                    {
                        SetShouldNotify(g);
                    }

                    parsed[league.Abbreviation] = games;
                }
            }

            if (parsed.Count == 0)
            {
                return;
            }

            foreach (var leagueAbbr in AllowedLeagues)
            {
                var games = parsed.TryGetValue(leagueAbbr, out var g)
                    ? g
                    : new List<MeatSportGame>();
                _gamesByLeague.AddOrUpdate(leagueAbbr, _ => games, (_, __) => games);
            }
        }

        private MeatSportGame ToGame(string league, Meat.Event ev)
        {
            if (ev?.Competitors == null || ev.Competitors.Count < 2)
            {
                return null;
            }

            var home = ev.Competitors.FirstOrDefault(c => c.HomeAway == "home")
                       ?? ev.Competitors[0];
            var away = ev.Competitors.FirstOrDefault(c => c.HomeAway == "away")
                       ?? ev.Competitors[1];

            var state = ev.FullStatus?.Type?.State;
            var detail = ev.FullStatus?.Type?.ShortDetail ?? "";
            var isOver = state == "post";
            var time = isOver ? "Final" : detail;

            return new MeatSportGame
            {
                League = league,
                HomeTeam = ShortLabel(home),
                HomeTeamFull = FullLabel(home),
                AwayTeam = ShortLabel(away),
                AwayTeamFull = FullLabel(away),
                HomeTeamScore = home.Score ?? "",
                AwayTeamScore = away.Score ?? "",
                Time = time,
                TimeLink = ev.Links?.FirstOrDefault()?.Href,
                IsOver = isOver
            };
        }

        private static string ShortLabel(Meat.Competitor c)
        {
            if (!string.IsNullOrWhiteSpace(c.Abbreviation))
            {
                return c.Abbreviation;
            }

            if (!string.IsNullOrWhiteSpace(c.Name))
            {
                return c.Name;
            }

            return c.DisplayName ?? "";
        }

        private static string FullLabel(Meat.Competitor c)
        {
            if (!string.IsNullOrWhiteSpace(c.Name))
            {
                return c.Name;
            }

            if (!string.IsNullOrWhiteSpace(c.DisplayName))
            {
                return c.DisplayName;
            }

            return c.Abbreviation ?? "";
        }

        private void SetShouldNotify(MeatSportGame game)
        {
            var key = $"{game.League}.{game.HomeTeam}.{game.AwayTeam}";

            if (_scoreCache.TryGetValue(key, out var prev))
            {
                if (!game.HomeTeamScore.Equals(prev.Item1))
                {
                    game.NotifyHome = true;
                }

                if (!game.AwayTeamScore.Equals(prev.Item2))
                {
                    game.NotifyAway = true;
                }
            }

            _scoreCache[key] = Tuple.Create(game.HomeTeamScore, game.AwayTeamScore);
        }

        private async Task<ScoreBoard> FetchScoreBoard(CancellationToken token)
        {
            try
            {
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApiUrl)))
                {
                    var response = await httpClient.SendAsync(request, token);
                    var responseString = await response.Content.ReadAsStringAsync();
                    return ScoreBoard.FromJson(responseString);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
