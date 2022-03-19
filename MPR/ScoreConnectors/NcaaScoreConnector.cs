using MPR.Models;
using MPR.NCAA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MPR.ScoreConnectors
{
    public class NcaaScoreConnector
    {
        public static NcaaScoreConnector Instance = new NcaaScoreConnector();
        private Bracket _currentBracket = new Bracket();

        public void Init(CancellationToken token)
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
                Name = "Ncaa Game Pull",
                Priority = ThreadPriority.Normal,
                IsBackground = true
            }.Start();
        }

        public NcaaBracket GetBracket(int clientOffset)
        {
            var bracket = _currentBracket;

            var games = bracket.Data.Games.Select(g => Wrap(g, clientOffset)).ToList();

            // We only want to show rounds where there are games where we know at least one of the teams
            var activeRounds = new HashSet<int>(games.Where(g => g.TeamOne.Name != "-" || g.TeamTwo.Name != "-").Select(g => g.RoundNumber));
            var rounds = bracket.Data.Tournaments.First().Rounds.Where(r => activeRounds.Contains(r.Number)).Select(Wrap).ToList();

            return new NcaaBracket
            {
                Rounds = rounds,
                Games = games
            };
        }

        private NcaaRound Wrap(Round r)
        {
            return new NcaaRound { Name = r.Name, Number = r.Number };
        }

        private NcaaGame Wrap(Game g, int clientOffset)
        {
            var teamOne = g.Teams.Count > 0 
                ? Wrap(g.Teams[0])
                : new NcaaTeam { Name = "-", Score = -1, Seed = null };

            var teamTwo = g.Teams.Count > 1
                ? Wrap(g.Teams[1])
                : new NcaaTeam { Name = "-", Score = -1, Seed = null };

            if(g.GameState == "F")
            {
                teamOne.Winner = teamOne.Score > teamTwo.Score;
                teamTwo.Winner = teamOne.Score < teamTwo.Score;
            }

            return new NcaaGame
            {
                RoundNumber = g.Round.Number,
                State = g.GameState,
                Period = g.Period,
                Clock = g.Clock,
                StartTime = GetClientTime(g.StartTimeEpoch, clientOffset),
                TeamOne = teamOne,
                TeamTwo = teamTwo
            };
        }

        private NcaaTeam Wrap(NCAA.Team t)
        {
            return new NcaaTeam
            {
                Name = $"({t.Seed}) {t.Name}",
                Score = t.Score ?? -1,
                Seed = t.Seed
            };
        }

        private DateTime GetClientTime(long dateMs, int clientOffset)
        {
            // Offset will be positive for timezones behind UTC
            DateTime clientTime = DateTimeOffset.FromUnixTimeSeconds(dateMs).ToUniversalTime().DateTime.AddMinutes(-clientOffset);
            return clientTime;
        }

        private async Task UpdateGames(CancellationToken token)
        {
            try
            {
                Bracket b = await FetchBracket(token);
                if(b?.Data?.Tournaments == null)
                {
                    if(_currentBracket != null)
                    {
                        return;
                    }

                    b = DefaultBracket();
                }

                Interlocked.Exchange(ref _currentBracket, b);
            }
            catch
            {
                // Ignore
            }
        }

        private async Task<Bracket> FetchBracket(CancellationToken token)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var uri = new Uri("https://sdataprod.ncaa.com/?operationName=scores_bracket_web&variables={\"seasonYear\":2021}&extensions={\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"f21cac8420a55a7d190f2f686a441e2507d8fb80f25eac5c91131ddd9df588da\"}}");
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        var response = await httpClient.SendAsync(request, token);
                        var responseString = await response.Content.ReadAsStringAsync();
                        var bracket = Bracket.FromJson(responseString);
                        return bracket;
                    }
                }
            }
            catch
            {
                return DefaultBracket();
            }
        }

        private Bracket DefaultBracket()
        {
            return new Bracket
            {
                Data = new Data
                {
                    Games = new List<Game>
                        {
                            new Game
                            {
                                StartTimeEpoch = 1647384000,
                                GameState = "P",
                                Round = new Round { Name = "Unknown" },
                                Teams = new List<NCAA.Team>
                                {
                                    new NCAA.Team { Name = "Team 1", Score = -1 },
                                    new NCAA.Team { Name = "Team 2", Score = -1 }
                                }
                            }
                        },
                    Tournaments = new List<Tournament>
                    {
                        new Tournament { Rounds = new List<Round> { new Round { Name = "Unknown" } } }
                    }
                }
            };
        }
    }
}