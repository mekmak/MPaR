using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MPR.Models;
using MPR.WorldCup;
using Match = MPR.WorldCup.Match;
using Team = MPR.WorldCup.Team;

namespace MPR.Connectors
{
    public class WorldCupConnector : Connector
    {
        public static WorldCupConnector Instance = new WorldCupConnector();
        private List<Match> _currentMatches = new List<Match>();

        private const string CalendarUrl =
            "https://api.fifa.com/api/v3/calendar/matches?language=en&count=500&idSeason=285023";

        private const string MatchCentreBase =
            "https://www.fifa.com/en/match-centre/match";

        // Hardcoded FIFA team page URLs, keyed by the team name returned by the calendar API.
        // Sourced from https://cxm-api.fifa.com/fifaplusweb/api/sections/teamsModule/4v5Yng3VdGD9c1cpnOIff1
        private static readonly Dictionary<string, string> TeamPageUrls =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Algeria", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/algeria" },
            { "Argentina", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/argentina" },
            { "Australia", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/australia" },
            { "Austria", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/austria" },
            { "Belgium", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/belgium" },
            { "Bosnia and Herzegovina", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/bosnia-herzegovina" },
            { "Brazil", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/brazil" },
            { "Cabo Verde", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/cabo-verde" },
            { "Canada", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/canada" },
            { "Colombia", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/colombia" },
            { "Congo DR", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/congo-dr" },
            { "Croatia", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/croatia" },
            { "Curaçao", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/curacao" },
            { "Czechia", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/czechia" },
            { "Côte d'Ivoire", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/cote-d-ivoire" },
            { "Ecuador", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/ecuador" },
            { "Egypt", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/egypt" },
            { "England", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/england" },
            { "France", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/france" },
            { "Germany", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/germany" },
            { "Ghana", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/ghana" },
            { "Haiti", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/haiti" },
            { "IR Iran", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/ir-iran" },
            { "Iraq", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/iraq" },
            { "Japan", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/japan" },
            { "Jordan", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/jordan" },
            { "Korea Republic", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/korea-republic" },
            { "Mexico", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/mexico" },
            { "Morocco", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/morocco" },
            { "Netherlands", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/netherlands" },
            { "New Zealand", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/new-zealand" },
            { "Norway", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/norway" },
            { "Panama", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/panama" },
            { "Paraguay", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/paraguay" },
            { "Portugal", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/portugal" },
            { "Qatar", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/qatar" },
            { "Saudi Arabia", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/saudi-arabia" },
            { "Scotland", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/scotland" },
            { "Senegal", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/senegal" },
            { "South Africa", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/south-africa" },
            { "Spain", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/spain" },
            { "Sweden", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/sweden" },
            { "Switzerland", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/switzerland" },
            { "Tunisia", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/tunisia" },
            { "Türkiye", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/turkiye" },
            { "USA", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/usa" },
            { "Uruguay", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/uruguay" },
            { "Uzbekistan", "https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/teams/uzbekistan" },
        };

        // Short codes for the 16 host cities, keyed by the CityName the calendar API returns.
        private static readonly Dictionary<string, string> CityCodes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Atlanta", "ATL" },
            { "Boston", "BOS" },
            { "Dallas", "DAL" },
            { "Guadalajara", "GDL" },
            { "Houston", "HOU" },
            { "Kansas City", "KC" },
            { "Los Angeles", "LA" },
            { "Mexico City", "CDMX" },
            { "Miami", "MIA" },
            { "Monterrey", "MONT" },
            { "New York", "NJ" },
            { "Philadelphia", "PHI" },
            { "San Francisco Bay Area", "SF" },
            { "Seattle", "SEA" },
            { "Toronto", "TOR" },
            { "Vancouver", "VAN" },
        };

        public void Init(CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var pulls = new[]
            {
                new Pull { Name = "World Cup Schedule", Task = UpdateMatches }
            };

            StartPulls(token, pulls);
        }

        public WorldCupSchedule GetSchedule(int clientOffset)
        {
            var matches = _currentMatches;

            var byDay = matches
                .GroupBy(m => m.DateUtc.AddMinutes(-clientOffset).Date)
                .OrderBy(g => g.Key)
                .ToList();

            int completedCounter = 1;
            int upcomingCounter = int.MinValue + 1;

            var days = new List<MatchDay>();
            foreach (var group in byDay)
            {
                bool isComplete = group.All(IsComplete);
                int order = isComplete ? completedCounter++ : upcomingCounter++;

                days.Add(new MatchDay
                {
                    Date = group.Key,
                    Order = order,
                    Matches = group
                        .OrderBy(m => m.DateUtc)
                        .Select(m => Wrap(m, clientOffset))
                        .ToList()
                });
            }

            if (completedCounter > 1)
            {
                var lastCompleted = days
                    .Where(d => d.Order > 0)
                    .OrderByDescending(d => d.Order)
                    .First();
                lastCompleted.Order = int.MinValue;
            }

            return new WorldCupSchedule { Days = days };
        }

        private static bool IsComplete(Match m)
        {
            return m.MatchStatus == 0;
        }

        private static bool IsLive(Match m)
        {
            return m.MatchStatus == 3;
        }

        private static WorldCupMatch Wrap(Match m, int clientOffset)
        {
            var groupFull = m.GroupName?.FirstOrDefault()?.Description;
            var cityFull = m.Stadium?.CityName?.FirstOrDefault()?.Description;

            return new WorldCupMatch
            {
                GroupFull = groupFull,
                GroupShort = GroupShort(groupFull),
                LocationFull = cityFull,
                LocationShort = CityShort(cityFull),
                HomeFull = HomeFull(m),
                HomeShort = HomeShort(m),
                HomeLink = TeamLink(m.Home),
                AwayFull = AwayFull(m),
                AwayShort = AwayShort(m),
                AwayLink = TeamLink(m.Away),
                HomeScore = m.HomeTeamScore,
                AwayScore = m.AwayTeamScore,
                StartDate = m.DateUtc.AddMinutes(-clientOffset),
                MatchLink = MatchLink(m),
                Status = GetStatus(m)
            };
        }

        private static string GroupShort(string groupFull)
        {
            if (string.IsNullOrEmpty(groupFull))
            {
                return null;
            }

            // "Group A" -> "A"
            const string prefix = "Group ";
            return groupFull.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? groupFull.Substring(prefix.Length)
                : groupFull;
        }

        private static string CityShort(string cityFull)
        {
            if (string.IsNullOrEmpty(cityFull))
            {
                return null;
            }

            return CityCodes.TryGetValue(cityFull, out var code) ? code : cityFull;
        }

        private static string MatchLink(Match m)
        {
            if (string.IsNullOrEmpty(m.IdCompetition) || string.IsNullOrEmpty(m.IdSeason)
                || string.IsNullOrEmpty(m.IdStage) || string.IsNullOrEmpty(m.IdMatch))
            {
                return null;
            }

            return $"{MatchCentreBase}/{m.IdCompetition}/{m.IdSeason}/{m.IdStage}/{m.IdMatch}";
        }

        private static string TeamLink(Team team)
        {
            var name = team?.TeamName?.FirstOrDefault()?.Description;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return TeamPageUrls.TryGetValue(name, out var url) ? url : null;
        }

        private static string HomeFull(Match m)
        {
            return m.Home?.TeamName?.FirstOrDefault()?.Description ?? m.PlaceHolderA ?? "TBD";
        }

        private static string HomeShort(Match m)
        {
            return m.Home?.Abbreviation ?? m.PlaceHolderA ?? "TBD";
        }

        private static string AwayFull(Match m)
        {
            return m.Away?.TeamName?.FirstOrDefault()?.Description ?? m.PlaceHolderB ?? "TBD";
        }

        private static string AwayShort(Match m)
        {
            return m.Away?.Abbreviation ?? m.PlaceHolderB ?? "TBD";
        }

        private static string GetStatus(Match m)
        {
            if (IsComplete(m) && m.HomeTeamScore.HasValue && m.AwayTeamScore.HasValue)
            {
                return $"{m.HomeTeamScore}-{m.AwayTeamScore}";
            }

            if (IsLive(m) && m.HomeTeamScore.HasValue && m.AwayTeamScore.HasValue)
            {
                return $"{m.HomeTeamScore}-{m.AwayTeamScore} LIVE";
            }

            return "-";
        }

        private async Task UpdateMatches(CancellationToken token)
        {
            var matches = await FetchMatches(token);
            _currentMatches = _currentMatches.Any()
                ? matches.Any() ? matches : _currentMatches
                : matches;
        }

        private async Task<List<Match>> FetchMatches(CancellationToken token)
        {
            try
            {
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(CalendarUrl)))
                {
                    var response = await httpClient.SendAsync(request, token);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var calendar = FifaCalendar.FromJson(responseString);
                    return calendar?.Results ?? new List<Match>();
                }
            }
            catch
            {
                return new List<Match>();
            }
        }
    }
}
