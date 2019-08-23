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

        private volatile List<Game> _currentGames = new List<Game>();

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

        public List<Game> GetGames()
        {
            return new List<Game>(_currentGames);
        }

        private async void UpdateGames()
        {
            while (true)
            {
                List<Game> currentGames = GetCurrentGames();
                _currentGames = currentGames;
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private List<Game> GetCurrentGames()
        {
            try
            {
                Schedule schedule = FetchSchedule();
                Week week = GetCurrentWeek(schedule);
                if (week == null)
                {
                    return new List<Game>();
                }

                var games = week.Matches.Select(m => new Game
                {
                    HomeTeam = m.Competitors[0].Name,
                    HomeTeamScore = GetScore(m, 0),
                    NotifyHome = ShouldNotify(m, 0),

                    AwayTeam = m.Competitors[1].Name,
                    AwayTeamScore = GetScore(m, 1),
                    NotifyAway = ShouldNotify(m, 1),

                    Time = GetTime(m)

                }).ToList();

                return games;
            }
            catch
            {
                return new List<Game>();
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

        private String GetScore(Match m, int index)
        {
            string score = m.Scores[index].Value.ToString();
            string games = $"({string.Join("-", m.Games.Select(g => g.Points != null && g.Points.Count > index ? g.Points[index] : 0))})";
            return $"{score} {games}";
        }

        private String GetTime(Match m)
        {
            return MatchOver(m)
                ? "FINAL"
                : DateTimeOffset.FromUnixTimeMilliseconds(m.StartDate).ToLocalTime().ToString("MMMM dd, HH:mm");
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