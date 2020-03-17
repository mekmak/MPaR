using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
            return currentWeeks.SelectMany(w => ToGames(w, clientOffset)).ToList();
        }

        #region UI 

        private List<OwlGame> ToGames(Week week, int clientOffset)
        {
            return week.Events.SelectMany(e => e.Matches.Select(m => ToGame(m, week, clientOffset))).ToList();
        }

        private OwlGame ToGame(Match match, Week week, int clientOffset)
        {
            var game = new OwlGame { Week = week.WeekNumber };

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

            // TODO - will  have to see what live games  return to do  this part
            //string games = $"({string.Join("-", m.Games.Select(g => g.Points != null && g.Points.Count > index ? g.Points[index] : 0))})";
            //return $"{score} {games}";
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

                List<Week> currentWeeks = await GetNearestWeeks();
                Interlocked.Exchange(ref _currentWeeks, currentWeeks);
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }

        private async Task<List<Week>> GetNearestWeeks()
        {
            try
            {
                List<Schedule> schedules = await FetchLatestSchedules();
                return schedules.Select(s => s.Content.Week).ToList();
            }
            catch
            {
                return new List<Week>();
            }
        }

        public const int CoronaWeek = 6; // Week skipped due to COVID-19
        public const int LastOwlWeek = 27;

        private async Task<List<Schedule>> FetchLatestSchedules()
        {
            int currentWeek = CultureInfo.CurrentUICulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Saturday);
            int owlWeek = currentWeek - 6; // Tribal knowledge

            int[] weeksToDisplay = GetWeeksToFetch(owlWeek, 3, LastOwlWeek, new [] { CoronaWeek });
            List<Task<Schedule>> scheduleTasks = weeksToDisplay.Select(FetchSchedule).ToList();
            var schedules = await Task.WhenAll(scheduleTasks);
            return schedules.ToList();
        }
        
        /// <summary>
        /// Returns which weeks to display, skipping weeks that had no games
        /// </summary>
        public int[] GetWeeksToFetch(int owlWeek, int numOfWeeksToDisplay, int lastWeek, IEnumerable<int> weeksToSkipIn)
        {
            if (numOfWeeksToDisplay < 1)
            {
                return new int[0];
            }

            var weeksToSkip = new HashSet<int>(weeksToSkipIn);

            if (numOfWeeksToDisplay > lastWeek)
            {
                numOfWeeksToDisplay = lastWeek;
            }

            // We want 'this' week to be in the 'middle'
            int middleIndex = numOfWeeksToDisplay / 2;
            int firstWeekToDisplay = owlWeek - middleIndex;
            if (numOfWeeksToDisplay % 2 == 0)
            {
                // If there is an odd number of weeks to display, we have an even amount on each side
                // Otherwise, show more upcoming games than finished games
                firstWeekToDisplay++;
                middleIndex--;
            }

            // If it's still the beginning, just show the first x weeks
            if (firstWeekToDisplay < 1)
            {
                firstWeekToDisplay = 1;
            }

            // If it's near the end, show the last x weeks
            if (firstWeekToDisplay + numOfWeeksToDisplay > lastWeek)
            {
                firstWeekToDisplay = lastWeek - numOfWeeksToDisplay + 1;
            }

            
            var weeks = new LinkedList<int>();

            int backSkipCount = 0;
            int missingBehind = 0;
            for (int index = middleIndex - 1; index >= 0; index--)
            {
                int week = firstWeekToDisplay + index - backSkipCount;
                while (weeksToSkip.Contains(week))
                {
                    backSkipCount++;
                    week--;
                }

                if (week > 0)
                {
                    weeks.AddFirst(week);
                }
                else
                {
                    missingBehind++;
                }
            }

            int frontSkipCount = 0;
            int missingInFront = 0;
            for(int index = middleIndex; index < numOfWeeksToDisplay; index++)
            {
                int week = firstWeekToDisplay + index + frontSkipCount;
                while (weeksToSkip.Contains(week))
                {
                    frontSkipCount++;
                    week++;
                }

                if (week <= lastWeek)
                {
                    weeks.AddLast(week);
                }
                else
                {
                    missingInFront++;
                }
            }

            if (missingBehind > 0 ^ missingInFront > 0)
            {
                if (missingBehind > 0)
                {
                    int nextValue = weeks.Last.Value + 1;
                    while (weeks.Count < numOfWeeksToDisplay && missingBehind > 0 && nextValue <= lastWeek)
                    {
                        if (weeksToSkip.Contains(nextValue))
                        {
                            nextValue++;
                            continue;
                        }

                        weeks.AddLast(nextValue);
                        nextValue++;
                        missingBehind--;
                    }
                }

                if (missingInFront > 0)
                {
                    int nextValue = weeks.First.Value - 1;
                    while (weeks.Count < numOfWeeksToDisplay && missingInFront > 0 && nextValue > 0)
                    {
                        if (weeksToSkip.Contains(nextValue))
                        {
                            nextValue--;
                            continue;
                        }

                        weeks.AddFirst(nextValue);
                        nextValue--;
                        missingBehind--;
                    }
                }
            }

            return weeks.ToArray();
        }

        private async Task<Schedule> FetchSchedule(int weekNumber)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var uri = new Uri($"https://wzavfvwgfk.execute-api.us-east-2.amazonaws.com/production/owl/paginator/schedule?stage=regular_season&season=2020&locale=en-us&page={weekNumber}");
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        request.Headers.Add("Referer", "https://overwatchleague.com/en-us/schedule?stage=regular_season");
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
                            WeekNumber = weekNumber,
                            Events = new List<Event>
                            {
                                new Event { Matches = new List<Match>()}
                            }
                        }
                    }
                };
            }
        }

        #endregion
    }
}