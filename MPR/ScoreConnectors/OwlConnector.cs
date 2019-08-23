using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MPR.Models.Games;
using MPR.Owl;

namespace MPR.ScoreConnectors
{
    public class OwlConnector
    {
        private OwlConnector() { }

        public static OwlConnector Instance = new OwlConnector();
        private volatile List<Match> _currentMatches = new List<Match>();

        public void InitGameDownload()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var thread = new Thread(UpdateGames)
            {
                Name = "Owl Game Pull",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            };
            thread.Start();
        }

        public List<Game> GetGames(int clientOffset)
        {
            return _currentMatches.Select(m => ToGame(m, clientOffset)).ToList();
        }

        private async void UpdateGames()
        {
            while (true)
            {
                List<Match> currentMatches = GetCurrentMatches();
                _currentMatches = currentMatches;
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private Game ToGame(Match m, int clientOffset)
        {
            var game = new Game
            {
                HomeTeam = m.Competitors[0].AbbreviatedName,
                HomeTeamLink = GetLink(m.Competitors[0]),
                HomeTeamScore = GetScore(m, 0),
                NotifyHome = ShouldNotify(m, 0),

                AwayTeam = m.Competitors[1].AbbreviatedName,
                AwayTeamLink = GetLink(m.Competitors[1]),
                AwayTeamScore = GetScore(m, 1),
                NotifyAway = ShouldNotify(m, 1),

                Time = GetTime(m, clientOffset),
                TimeLink = GetLink(m)
            };

            return game;
        }

        private string GetLink(Competitor competitor)
        {
            return $"https://overwatchleague.com/en-us/teams/{competitor.Id}";
        }

        private string GetLink(Match match)
        {
            return $"https://overwatchleague.com/en-us/match/{match.Id}";
        }

        private List<Match> GetCurrentMatches()
        {
            try
            {
                Schedule schedule = FetchSchedule();
                Week week = GetCurrentWeek(schedule);
                if (week == null)
                {
                    return new List<Match>();
                }

                return week.Matches;
            }
            catch
            {
                return new List<Match>();
            }
        }

        private bool ShouldNotify(Match match, int i)
        {
            if (!MatchOver(match))
            {
                return false;
            }

            int other = i == 0 ? 1 : 0;
            return match.Scores[i].Value > match.Scores[other].Value;
        }

        private string GetScore(Match m, int index)
        {
            string score = m.Scores[index].Value.ToString();
            string games = $"({string.Join("-", m.Games.Select(g => g.Points != null && g.Points.Count > index ? g.Points[index] : 0))})";
            return $"{score} {games}";
        }

        private string GetTime(Match m, int clientOffset)
        {
            return MatchOver(m)
                ? m.Games.Count > 4 ? "FINAL (OT)" : "FINAL"
                : GetClientTime(m.StartDate, clientOffset).ToString("ddd dd, HH:mm");
        }

        private DateTime GetClientTime(long dateMs, int clientOffset)
        {
            // Offset will be positive for timezones behind UTC
            DateTime clientTime = DateTimeOffset.FromUnixTimeMilliseconds(dateMs).ToUniversalTime().DateTime.AddMinutes(-clientOffset);
            return clientTime;
        }

        private bool MatchOver(Match m)
        {
            return m.Status.Equals("CONCLUDED");
        }

        private Schedule FetchSchedule()
        {
            return FetchScheduleAsync().Result;
        }

        private async Task<Schedule> FetchScheduleAsync()
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(@"https://api.overwatchleague.com/schedule");
            string content = await response.Content.ReadAsStringAsync();
            Schedule schedule = Schedule.FromJson(content);
            return schedule;
        }

        private Week GetCurrentWeek(Schedule schedule)
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            List<Week> weeks = schedule.Data.Stages
                .SelectMany(s => s.Weeks)
                .Where(w => w.EndDate >= now)
                .OrderBy(w => w.StartDate)
                .ToList();

            return weeks.FirstOrDefault();
        }
    }
}