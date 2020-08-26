using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MPR.Models.Games;
using MPR.Owl.V2;

namespace MPR.ScoreConnectors
{
    public class OwlConnectorV2
    {
        public static OwlConnectorV2 Instance = new OwlConnectorV2();
        private List<Week> _currentWeeks = new List<Week>();

        public void InitGameDownload(CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var thread = new Thread(() => UpdateGames(token))
            {
                Name = "Owl Game Pull V2",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            };
            thread.Start();
        }

        public List<OwlGame> GetGames(int clientOffset)
        {
            var currentWeeks = new List<Week>(_currentWeeks);
            return currentWeeks
                .OrderBy(w => w.WeekName.Contains("Playoffs") ? 1 : 0)
                .ThenBy(w => w.WeekNumber)
                .SelectMany(w => ToGames(w, clientOffset))
                .ToList();
        }

        #region UI 

        private List<OwlGame> ToGames(Week week, int clientOffset)
        {
            return week.Events.SelectMany(e => e.Matches.Select(m => ToGame(m, week, clientOffset))).ToList();
        }

        private OwlGame ToGame(Match match, Week week, int clientOffset)
        {
            var game = new OwlGame
            {
                WeekName = week.WeekName,
                WeekNumber = week.WeekNumber
            };

            var homeTeam = new Team(match.Competitors.Count > 0 ? match.Competitors[0] : null);
            var awayTeam = new Team(match.Competitors.Count > 1 ? match.Competitors[1] : null);

            game.HomeTeam = homeTeam.Name;
            game.HomeTeamLink = homeTeam.Link;
            game.AwayTeam = awayTeam.Name;
            game.AwayTeamLink = awayTeam.Link;

            if (match.Scores != null && match.Scores.Any())
            {
                game.HomeTeamScore = GetMatchScore(match, 0);
                game.HomeTeamWon = ShouldNotify(match, 0);
                game.AwayTeamScore = GetMatchScore(match, 1);
                game.AwayTeamWon = ShouldNotify(match, 1);
            }

            game.Time = GetTime(match, clientOffset);
            game.TimeLink = $"https://overwatchleague.com/en-us/match/{match.Id}";
            game.LiveLink = MatchLive(match) ? "https://www.youtube.com/overwatchleague" : null;

            return game;
        }

        private bool ShouldNotify(Match match, int i)
        {
            if (!MatchOver(match))
            {
                return false;
            }

            int other = i == 0 ? 1 : 0;
            return match.Scores[i] > match.Scores[other];
        }

        private string GetMatchScore(Match m, int index)
        {
            if (MatchPending(m))
            {
                return "-";
            }

            string score = m.Scores[index].ToString();
            if (MatchOver(m))
            {
                return score;
            }

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
            return m.IsLive;
        }

        private static bool MatchOver(Match m)
        {
            return m.Status.Equals("CONCLUDED");
        }

        private static bool MatchPending(Match m)
        {
            return !MatchLive(m) && !MatchOver(m);
        }

        private class Team
        {
            public Team(Competitor competitor)
            {
                Name = competitor == null ? "---" : competitor.AbbreviatedName;
                Link = "https://overwatchleague.com/en-us/teams/";
            }

            public string Name { get; }
            public string Link { get; }
        }

        #endregion

        #region Data Fetch

        private async void UpdateGames(CancellationToken token)
        {
            while(true)
            {
                if(token.IsCancellationRequested)
                {
                    return;
                }

                List<Week> currentWeeks;
                try
                {
                    currentWeeks = await FetchLatestWeeks();
                }
                catch
                {
                    currentWeeks = new List<Week>();
                }

                Interlocked.Exchange(ref _currentWeeks, currentWeeks);
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }

        private enum Direction
        {
            Forward,
            Back
        }

        private static async Task<List<Week>> FetchLatestWeeks()
        {
            WeekNumber owlWeek = GetCurrentOwlWeek();
            var backTask = FetchWeeks(GetPreviousWeek(owlWeek), 1, Direction.Back);
            var forwardTask = FetchWeeks(owlWeek, 2, Direction.Forward);
            var weeks = await Task.WhenAll(backTask, forwardTask);
            return weeks.SelectMany(w => w).ToList();
        }

        private const int LastRegularSeasonWeek = 29;
        private static WeekNumber GetCurrentOwlWeek()
        {
            int currentWeek = CultureInfo.CurrentUICulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Saturday);
            int owlWeek = currentWeek - 6; // Tribal knowledge
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

        private static async Task<List<Week>> FetchWeeks(WeekNumber weekNumber, int numberOfWeeks, Direction direction)
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

                Week week = await FetchWeek(weekNumber) ?? await FetchWeek(weekNumber); // retry once
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

        private static async Task<Week> FetchWeek(WeekNumber week)
        {
            try
            {
                Schedule schedule = await FetchSchedule(week);
                return schedule?.Content?.Week;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Schedule> FetchSchedule(WeekNumber weekNumber)
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
                        var response = await httpClient.SendAsync(request);
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
                    return $"https://wzavfvwgfk.execute-api.us-east-2.amazonaws.com/production/owl/paginator/schedule?stage=regular_season&season=2020&locale=en-us&page={weekNumber.Number}";
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