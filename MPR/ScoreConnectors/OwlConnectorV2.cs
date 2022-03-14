using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MPR.Models;
using MPR.Extensions;
using MPR.Owl.V2;
using HtmlAgilityPack;

namespace MPR.ScoreConnectors
{
    public class OwlConnectorV2
    {
        public static OwlConnectorV2 Instance = new OwlConnectorV2();
        private List<Week> _currentWeeks = new List<Week>();
        private List<Tournament> _tournaments = new List<Tournament>();

        public void InitGameDownload(CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            UpdateGames(token).Wait();
            new Thread(async () => 
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await UpdateGames(token);
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
            })
            {
                Name = "Owl Game Pull V2",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            }.Start();

            UpdateStandings(token).Wait();
            new Thread(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await UpdateStandings(token);
                    await Task.Delay(TimeSpan.FromMinutes(10), token);
                }
            })
            {
                Name = "Owl Standings Pull V2",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            }.Start();
        }

        #region UI 

        public List<OwlGame> GetGames(int clientOffset)
        {
            var currentWeeks = new List<Week>(_currentWeeks);
            return currentWeeks
                .OrderBy(w => w.WeekName.Contains("Playoffs") ? 1 : 0)
                .ThenBy(w => w.WeekNumber)
                .SelectMany(w => ToGames(w, clientOffset))
                .DistinctBy(g => g.Id)
                .ToList();
        }

        private static readonly Dictionary<string, int> _tournamentIndexes = new Dictionary<string, int>
        {
            {"2021 Regular Season", 0},
            {"Countdown Cup: Qualifiers", 1},
            {"Summer Showdown: Qualifiers", 2},
            {"June Joust: Qualifiers", 3},
            {"May Melee Qualifiers", 4},
        };

        public List<Models.Standings> GetStandings()
        {
            var tournaments = new List<Tournament>(_tournaments);
            var standings = tournaments
                // Don't want to display qualifiers that haven't started yet
                .Where(tr => tr.Regions.Any(r => r.Teams.Any(t => t.MapsPlayed > 0)))
                .Select(Wrap)
                .Where(s => s != null) 
                .ToList();

            List<Models.Standings> ordered = standings.OrderBy(t => _tournamentIndexes.TryGetValue(t.TournamentName, out var index) ? index : int.MaxValue).ToList();
            return ordered;
        }

        private Models.Standings Wrap(Tournament t)
        {
            if (t == null)
            {
                return null;
            }

            return new Models.Standings
            {
                TournamentName = t.Title,
                Regions = t?.Regions.Select(Wrap).ToList() ?? new List<Models.Region>()
            };
        }

        private Models.Region Wrap(Owl.V2.Region r)
        {
            bool madeIt = r.Teams.Any(t => t.Name == null);

            var teams = new List<Models.Team>();
            foreach (var t in r.Teams)
            {
                if (t.Name == null)
                {
                    madeIt = false;
                    continue;
                }

                teams.Add(new Models.Team
                {
                    Name = t.Name,
                    Rank = t.Rank,
                    Wins = t.Wins,
                    Loses = t.Loses,
                    MapDiff = t.MapDiff ?? "",
                    Points = t.Points,
                    WinLoss = t.WinLoss,
                    MakesCutoff = madeIt,
                    TeamUrl = t.TeamUrl
                });
            }

            return new Models.Region
            {
                Name = r.Name,
                Teams = teams ?? new List<Models.Team>()
            };
        }

        private List<OwlGame> ToGames(Week week, int clientOffset)
        {
            return week.Events.SelectMany(e => e.Matches).Select(m => ToGame(m, week, clientOffset)).ToList();
        }

        private OwlGame ToGame(Match match, Week week, int clientOffset)
        {
            var game = new OwlGame
            {
                Id = match.Id,
                WeekName = week.WeekName,
                WeekNumber = week.WeekNumber
            };

            var homeTeam = new Team(match.Competitors.Count > 0 ? match.Competitors[0] : null);
            var awayTeam = new Team(match.Competitors.Count > 1 ? match.Competitors[1] : null);

            game.HomeTeam = homeTeam.Name;
            game.AwayTeam = awayTeam.Name;

            game.HomeTeamScore = GetMatchScore(match, 0);
            game.HomeTeamWon = ShouldNotify(match, 0);
            game.AwayTeamScore = GetMatchScore(match, 1);
            game.AwayTeamWon = ShouldNotify(match, 1);

            game.Time = GetTime(match, clientOffset);
            game.TimeLink = $"https://overwatchleague.com/en-us/match/{match.Id}";
            game.LiveLink = MatchLive(match) ? "https://www.youtube.com/overwatchleague" : null;

            return game;
        }

        private bool ShouldNotify(Match match, int i)
        {
            if (!MatchOver(match) || !ScoresAvailable(match))
            {
                return false;
            }

            int other = i == 0 ? 1 : 0;
            return match.Scores[i] > match.Scores[other];
        }

        private string GetMatchScore(Match m, int index)
        {
            if (!ScoresAvailable(m))
            {
                return "-";
            }

            string score = m.Scores[index].ToString();
            return score;
        }

        private string GetTime(Match m, int clientOffset)
        {
            if(MatchLive(m))
            {
                return "Live";
            }

            if (!m.StartDateUnix.HasValue)
            {
                return "Unknown";
            }

            if(MatchOver(m))
            {
                return "Final";
            }

            return GetClientTime(m.StartDateUnix.Value, clientOffset).ToString("ddd dd, HH:mm");
        }

        private DateTime GetClientTime(long dateMs, int clientOffset)
        {
            // Offset will be positive for timezones behind UTC
            DateTime clientTime = DateTimeOffset.FromUnixTimeMilliseconds(dateMs).ToUniversalTime().DateTime.AddMinutes(-clientOffset);
            return clientTime;
        }

        private static bool MatchLive(Match m)
        {
            return m.Status.Equals("LIVE");
        }

        private static bool MatchOver(Match m)
        {
            return m.Status.Equals("CONCLUDED");
        }

        private static bool MatchPending(Match m)
        {
            return m.Status.Equals("PENDING");
        }

        private static bool ScoresAvailable(Match m)
        {
            if(MatchPending(m))
            {
                return false;
            }

            return m?.Scores.Any() ?? false;
        }

        private class Team
        {
            public Team(Competitor competitor)
            {
                Name = competitor == null ? "---" : competitor.AbbreviatedName;
            }

            public string Name { get; }
        }

        #endregion

        #region Data Fetch

        private async Task UpdateStandings(CancellationToken token)
        {
            try
            {
                List<Tournament> tournaments = await FetchLatestStandings(token);
                Interlocked.Exchange(ref _tournaments, tournaments);
            }
            catch
            {
                // Ignore
            }            
        }

        private async Task<List<Owl.V2.Tournament>> FetchLatestStandings(CancellationToken token)
        {
            var uri = new Uri("https://overwatchleague.com/en-us/standings");
            using(var request = new HttpRequestMessage(HttpMethod.Get, uri))
            using (var client = new HttpClient())
            {
                var resp = await client.SendAsync(request, token);
                var content = await resp.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                HtmlNode node = doc.GetElementbyId("__NEXT_DATA__");
                var sr = StandingsResponse.FromJson(node.InnerText);

                return sr?.Props?.PageProps?.Blocks?
                    .Where(b => b.Standings != null)
                    .SelectMany(b => b.Standings?.Tournaments).ToList() ?? new List<Tournament>();
            }        
        }

        private async Task UpdateGames(CancellationToken token)
        {
            try
            {
                List<Week> currentWeeks = await FetchLatestWeeks(token);
                Interlocked.Exchange(ref _currentWeeks, currentWeeks);
            }
            catch
            {
                // Ignore
            }            
        }

        private enum Direction
        {
            Forward,
            Back
        }

        private static async Task<List<Week>> FetchLatestWeeks(CancellationToken token)
        {
            WeekNumber owlWeek = GetCurrentOwlWeek();
            var backTask = FetchWeeks(GetPreviousWeek(owlWeek), 1, Direction.Back, token);
            var forwardTask = FetchWeeks(owlWeek, 2, Direction.Forward, token);
            var weeks = await Task.WhenAll(backTask, forwardTask);
            return weeks.SelectMany(w => w).ToList();
        }

        private const int LastRegularSeasonWeek = 29;
        private static WeekNumber GetCurrentOwlWeek()
        {
            int currentWeek = CultureInfo.CurrentUICulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Wednesday);
            int owlWeek = Math.Max(1, currentWeek - 15); // Tribal knowledge

            return owlWeek <= LastRegularSeasonWeek 
                ? WeekNumber.RegularSeason(owlWeek) 
                : WeekNumber.Playoffs(owlWeek - LastRegularSeasonWeek);
        }

        private static WeekNumber GetPreviousWeek(WeekNumber weekNumber)
        {
            switch (weekNumber.Type)
            {
                case WeekType.Playoffs:
                {
                    return weekNumber.Number > 1
                        ? WeekNumber.Playoffs(weekNumber.Number - 1)
                        : WeekNumber.RegularSeason(LastRegularSeasonWeek);
                }
                case WeekType.RegularSeason:
                {
                    return WeekNumber.RegularSeason(Math.Max(1, weekNumber.Number - 1));
                }
                default:
                {
                    throw new ArgumentException($"Cannot get previous week, unsupported week type '{weekNumber.Type}'");
                }
            }
        }

        private static WeekNumber GetNextWeek(WeekNumber weekNumber)
        {
            switch (weekNumber.Type)
            {
                case WeekType.Playoffs:
                {
                    return WeekNumber.Playoffs(weekNumber.Number + 1);
                }
                case WeekType.RegularSeason:
                {
                    return weekNumber.Number >= LastRegularSeasonWeek
                        ? WeekNumber.Playoffs(1)
                        : WeekNumber.RegularSeason(weekNumber.Number + 1);
                }
                default:
                {
                    throw new ArgumentException($"Cannot get next week, unsupported week type '{weekNumber.Type}'");
                }
            }
        }

        private static async Task<List<Week>> FetchWeeks(WeekNumber weekNumber, int numberOfWeeks, Direction direction, CancellationToken token)
        {
            Func<WeekNumber, WeekNumber> incrementer;
            switch (direction)
            {
                case Direction.Forward:
                    incrementer = GetNextWeek;
                    break;
                case Direction.Back:
                    incrementer = GetPreviousWeek;
                    break;
                default:
                    throw new ArgumentException($"Unhandled fetch direction '{direction}");
            }

            int nullWeeksEncountered = 0;
            var weeks = new List<Week>();

            while (weeks.Count < numberOfWeeks && nullWeeksEncountered < 3)
            {
                if (weekNumber.Number < 1)
                {
                    break;
                }

                Week week = await FetchWeek(weekNumber, token) ?? await FetchWeek(weekNumber, token) ?? await FetchWeek(weekNumber, token); // retry
                if (week != null)
                {
                    weeks.Add(week);
                }
                else
                {
                    nullWeeksEncountered++;
                }

                weekNumber = incrementer(weekNumber);
            }

            return weeks;
        }

        private static async Task<Week> FetchWeek(WeekNumber week, CancellationToken token)
        {
            try
            {
                Schedule schedule = await FetchSchedule(week, token);
                int? matchCount = schedule?.Content?.Week?.Events?.SelectMany(e => e.Matches).Count();
                return matchCount.HasValue && matchCount.Value > 0 ? schedule.Content.Week : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Schedule> FetchSchedule(WeekNumber weekNumber, CancellationToken token)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {                                     
                    var uri = new Uri(GetFetchUri(weekNumber));
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        request.Headers.Add("Referer", GetFetchReferrer(weekNumber));
                        request.Headers.Add("x-origin", "overwatchleague.com");
                        var response = await httpClient.SendAsync(request, token);
                        var responseString = await response.Content.ReadAsStringAsync();
                        var schedule = Schedule.FromJson(responseString);
                        return schedule;
                    }
                }
            }
            catch
            {
                return new Schedule
                {
                    Content = new Content
                    {
                        Week = new Week
                        {
                            WeekName = "Week 1",
                            WeekNumber = 1,
                            Events = new List<Event>
                            {
                                new Event { Matches = new List<Match>()}
                            }
                        }
                    }
                };
            }
        }

        private static string GetFetchUri(WeekNumber weekNumber)
        {
            switch (weekNumber.Type)
            {
                case WeekType.Playoffs:
                    return $"https://wzavfvwgfk.execute-api.us-east-2.amazonaws.com/production/owl/paginator/schedule?stage=regular_season&season=2020&locale=en-us&page={weekNumber.Number}&id=bltaea9843a2219186c";
                case WeekType.RegularSeason:
                    return $"https://pk0yccosw3.execute-api.us-east-2.amazonaws.com/production/v2/content-types/schedule/blt78de204ce428f00c/week/{weekNumber.Number}?locale=en-us";
                default:
                    throw new ArgumentException($"Cannot get fetch call URI, unrecognized week type '{weekNumber.Type}'");
            }
        }

        private static string GetFetchReferrer(WeekNumber weekNumber)
        {
            switch (weekNumber.Type)
            {
                case WeekType.Playoffs:
                    return $"https://overwatchleague.com/en-us/2020-playoffs?stage=regular_season&week={weekNumber.Number}";
                case WeekType.RegularSeason:
                    return $"https://overwatchleague.com/en-us/schedule?stage=regular_season&week={weekNumber.Number}";
                default:
                    throw new ArgumentException($"Cannot get fetch call referrer, unrecognized week type '{weekNumber.Type}'");
            }
        }

        private enum WeekType
        {
            RegularSeason,
            Playoffs
        }

        private class WeekNumber
        {
            private WeekNumber() { }

            public WeekType Type { get; private set; }
            public int Number { get; private set; }

            public static WeekNumber Playoffs(int weekNumber)
            {
                return new WeekNumber { Number = weekNumber, Type = WeekType.Playoffs };
            }

            public static WeekNumber RegularSeason(int weekNumber)
            {
                return new WeekNumber { Number = weekNumber, Type = WeekType.RegularSeason };
            }
        }

        #endregion
    }
}