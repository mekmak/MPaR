using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MPR.F1;
using MPR.HTML;
using MPR.Models;
using Event = MPR.F1.Event;

namespace MPR.Connectors
{
    public class F1Connector : Connector
    {
        public static F1Connector Instance = new F1Connector();
        private List<Event> _currentEvents = new List<Event>();
        private F1TeamStandings _currentTeamStandings = new F1TeamStandings();
        private F1DriverStandings _currentDriverStandings = new F1DriverStandings();
        private F1RealDriverStandings _currentRealDriverStandings = new F1RealDriverStandings();
        private F1RealTeamStandings _currentRealTeamStandings = new F1RealTeamStandings();

        public void Init(CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var pulls = new[]
            {
                new Pull
                {
                    Name = "F1 Schedule", Task = UpdateScoreBoard
                },
                new Pull
                {
                    Name = "F1 Team Standings", Task = UpdateTeamStandings
                },
                new Pull
                {
                    Name = "F1 Driver Standings", Task = UpdateDriverStandings
                }
            };

            StartPulls(token, pulls);
        }

        public Models.F1Standings GetStandings()
        {
            var teams = _currentTeamStandings.Teams.Select(Wrap).ToList();
            var realTeams = _currentRealTeamStandings.Teams.Select(Wrap).ToList();
            var drivers = _currentDriverStandings.Drivers.Select(Wrap).ToList();
            var realDrivers = _currentRealDriverStandings.Drivers.Select(Wrap).ToList();
            return new Models.F1Standings 
            { 
                TeamStandings = teams,
                DriverStandings = drivers,
                RealDriverStandings = realDrivers,
                RealTeamStandings = realTeams
            };
        }

        private static readonly Dictionary<string,string> TeamLongNames = new Dictionary<string, string>
        {
            {"Racing Bulls Honda RBPT", "Visa CashApp Honda RBPT"},
        };

        private static readonly Dictionary<string,string> TeamShortNames = new Dictionary<string, string>
        {
            {"Red Bull Racing Honda RBPT", "Red Bull"},
            {"Ferrari", "Ferrari"},
            {"Mercedes", "Mercedes"},
            {"McLaren Mercedes", "McLaren"},
            {"Kick Sauber Ferrari", "Kick Sauber"},
            {"Alpine Renault", "Alpine"},
            {"Racing Bulls Honda RBPT", "Visa CashApp"},
            {"Haas Ferrari", "Haas"},
            {"Aston Martin Aramco Mercedes", "Aston Martin"},
            {"Williams Mercedes", "Williams"}
        };

        private Models.F1Team Wrap(F1.F1Team team)
        {
            return new Models.F1Team
            {
                Name = TeamLongNames.TryGetValue(team.Name, out var name) ? name : team.Name,
                Points = team.Points,
                Position = team.Position,
            };
        }

        private Models.RealF1Team Wrap(F1.RealF1Team team)
        {
            return new Models.RealF1Team
            {
                Name = TeamLongNames.TryGetValue(team.Name, out var name) ? name : team.Name,
                Points = team.Points,
                Position = team.Position,
                FakePoints = team.FakePoints,
                FakePosition = team.FakePosition,
                PositionsMoved = team.PositionsMoved,
                PointsDiff = team.PointsDiff
            };
        }

        private Models.F1Driver Wrap(F1.F1Driver driver)
        {
            return new Models.F1Driver
            {
                Name = driver.Name,
                Points = driver.Points,
                Position= driver.Position,
                Nationality = driver.Nationality,
                Car = TeamShortNames.TryGetValue(driver.Car, out var shortName) ? shortName : driver.Car
            };
        }

        private Models.RealF1Driver Wrap(F1.RealF1Driver driver)
        {
            return new Models.RealF1Driver
            {
                Name = driver.Name,
                Position = driver.Position,
                FakePosition = driver.FakePosition,
                PositionsMoved = driver.PositionsMoved,
                Points = driver.Points,
                FakePoints = driver.FakePoints,
                PointsDiff = driver.PointsDiff,
                Nationality = driver.Nationality,
                Car = TeamShortNames.TryGetValue(driver.Car, out var shortName) ? shortName : driver.Car
            };
        }

        private static readonly HashSet<string> CompleteRaceSummaries = new HashSet<string> { "Final", "Canceled" };
        private bool IsComplete(Event e)
        {
            return CompleteRaceSummaries.Contains(e.Summary);
        }

        private static readonly HashSet<string> PointsScoringCompetitionTypes = new HashSet<string> { "Sprint Race", "Race" };
        private bool IsPointsScoring(Event e)
        {
            return PointsScoringCompetitionTypes.Contains(e.Type.Name);
        }

        public Models.F1Schedule GetSchedule(int clientOffset)
        {
            var races = new Dictionary<string, Models.Race>();

            int completedCounter = 1;
            int upcomingCounter = int.MinValue + 1;

            foreach (var currentEvent in _currentEvents.OrderBy(e => e.DateUtc))
            {
                if (!races.TryGetValue(currentEvent.Name, out var race))
                {
                    bool isComplete = _currentEvents
                        .Where(e => e.Name.Equals(currentEvent.Name))
                        .All(IsComplete);

                    // We want to first show all events that haven't finished yet,
                    // then all events that already finished
                    int order = isComplete
                        ? completedCounter++
                        : upcomingCounter++;

                    race = new Race
                    {
                        Order = order,
                        Name = currentEvent.Name,
                        Events = new List<Models.Event>(),
                        Link = currentEvent.Link
                    };

                    races[currentEvent.Name] = race;
                }

                race.Events.Add(Wrap(currentEvent, clientOffset));
            }

            if (completedCounter > 1)
            {
                // We want to show the last event that completed first,
                // assuming that there is at least one completed event
                var lastCompleted = races.Values
                    .OrderByDescending(r => r.Order)
                    .First();
                lastCompleted.Order = int.MinValue;
            }

            return new F1Schedule { Races = races.Values.ToList() };
        }

        private Models.Event Wrap(Event e, int clientOffset)
        {
            return new Models.Event
            {
                Name = GetEventName(e.Type),
                StartDate = e.DateUtc.AddMinutes(-clientOffset),
                Status = e.Status == "pre" ? "-" : e.Summary
            };
        }

        private static readonly Dictionary<string, string> EventShortNames = new Dictionary<string, string>
        {
            {"Sprint Qualifying", "SQ"}
        };

        private string GetEventName(CompetitionType ct)
        {
            if (ct == null)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(ct.ShortName))
            {
                return ct.ShortName;
            }

            return EventShortNames.TryGetValue(ct.Name, out var shortName) ? shortName : ct.Name;
        }

        private static readonly string[] RaceDates =
        {
            "20250316", // Australia
            "20250323", // China
            "20250406", // Japan
            "20250413", // Bahrain
            "20250420", // Saudi Arabia
            "20250504", // Miami
            "20250518", // Imola
            "20250525", // Monaco
            "20250601", // Spain
            "20250615", // Canada
            "20250629", // Austria
            "20250706", // Silverstone
            "20250727", // Belgium
            "20250803", // Hungary
            "20250831", // Netherlands
            "20250907", // Monza
            "20250921", // Baku
            "20251005", // Singapore
            "20251019", // COTA
            "20251026", // Mexico
            "20251109", // Brazil
            "20251122", // Vegas
            "20251130", // Qatar
            "20251207", // Abu Dhabi
        };

        private async Task UpdateTeamStandings(CancellationToken token)
        {
            var standings = await FetchTeamStandings(token);
            _currentTeamStandings = _currentTeamStandings.Teams.Any()
                ? standings.Teams.Any() ? standings : _currentTeamStandings
                : standings;
        }

        private async Task UpdateDriverStandings(CancellationToken token)
        {
            var standings = await FetchDriverStandings(token);
            _currentDriverStandings = _currentDriverStandings.Drivers.Any()
                ? standings.Drivers.Any() ? standings : _currentDriverStandings
                : standings;

            _currentRealDriverStandings = CalculateRealDriverStandings(_currentDriverStandings, _currentEvents);
            _currentRealTeamStandings = CalculateRealTeamStandings(_currentRealDriverStandings, _currentTeamStandings);
        }

        private F1RealTeamStandings CalculateRealTeamStandings(F1RealDriverStandings driverStandings, F1TeamStandings fakeTeamStandings)
        {
            var teamToPoints = new Dictionary<string, double>();
            foreach(var driver in driverStandings.Drivers)
            {
                if(!teamToPoints.ContainsKey(driver.Car))
                {
                    teamToPoints[driver.Car] = 0;
                }

                teamToPoints[driver.Car] += driver.Points;
            }

            var realTeams = new List<F1.RealF1Team>();
            foreach (F1.F1Team team in fakeTeamStandings.Teams)
            {
                var realPoints = teamToPoints.TryGetValue(team.Name, out double points) ? Math.Round(points, 2) : -1;
                var pointsDiff = Math.Round(realPoints == -1 ? 0 : realPoints - team.Points, 2);
                realTeams.Add(new F1.RealF1Team
                {
                    Name = team.Name,
                    FakePoints = team.Points,
                    FakePosition = team.Position,
                    PointsDiff = pointsDiff,
                    Points = realPoints,
                    Position = -1,
                    PositionsMoved = 0
                });                
            }

            realTeams = realTeams.OrderByDescending(t => t.Points != -1 ? t.Points : t.FakePoints).ToList();
            for(int i = 0; i < realTeams.Count; i++)
            {
                var realPosition = i + 1;
                realTeams[i].Position = realPosition;
                realTeams[i].PositionsMoved = realTeams[i].FakePosition - realPosition;
            }

            return new F1RealTeamStandings { Teams = realTeams };
        }

        private static readonly Dictionary<int, double> FakeRacePositionPoints = new Dictionary<int, double>
        {
            {1, 25},
            {2, 18},
            {3, 15},
            {4, 12},
            {5, 10},
            {6, 8},
            {7, 6},
            {8, 4},
            {9, 2},
            {10, 1}
        };

        private static readonly Dictionary<int, double> FakeSprintRacePositionPoints = new Dictionary<int, double>
        {
            {1, 8},
            {2, 7},
            {3, 6},
            {4, 5},
            {5, 4},
            {6, 3},
            {7, 2},
            {8, 1}
        };

        private static readonly Dictionary<int, double> RealRacePositionPoints = new Dictionary<int, double>
        {
            {1, 25},
            {2, 18},
            {3, 15},
            {4, 12},
            {5, 10},
            {6, 8},
            {7, 6},
            {8, 4},
            {9, 2},
            {10, 1},
            {11, 0.8},
            {12, 0.6},
            {13, 0.5},
            {14, 0.4},
            {15, 0.3},
            {16, 0.2},
            {17, 0.15},
            {18, 0.10},
            {19, 0.05}
        };

        private static readonly Dictionary<int, double> RealSprintRacePositionPoints = new Dictionary<int, double>
        {
            {1, 8},
            {2, 7},
            {3, 6},
            {4, 5},
            {5, 4},
            {6, 3},
            {7, 2},
            {8, 1},
            {9, 0.8},
            {10, 0.6},
            {11, 0.5},
            {12, 0.4},
            {13, 0.3},
            {14, 0.2},
            {15, 0.15},
            {16, 0.10},
            {17, 0.05}
        };

        private double CalculateRealPositionPoints(Event race, int position)
        {
            return race.Type.Name == "Race" 
                ? (RealRacePositionPoints.TryGetValue(position, out double rvalue) ? rvalue : 0)
                : (RealSprintRacePositionPoints.TryGetValue(position, out double srvalue) ? srvalue : 0);
        }

        private double CalculateFakePositionPoints(Event race, int position)
        {
            return race.Type.Name == "Race" 
                ? (FakeRacePositionPoints.TryGetValue(position, out double rvalue) ? rvalue : 0)
                : (FakeSprintRacePositionPoints.TryGetValue(position, out double srvalue) ? srvalue : 0);
        }

        private static readonly Dictionary<string, string> EnglishDammit = new Dictionary<string, string>
        {
            {"Nico Hülkenberg", "Nico Hulkenberg"},
            {"Andrea Kimi Antonelli", "Kimi Antonelli"}
        };

        private F1RealDriverStandings CalculateRealDriverStandings(F1DriverStandings fakeStandings, List<Event> races)
        {
            var nameToPoints = new Dictionary<string, double>();
            var nameToFakePoints = new Dictionary<string, double>();
            List<Event> relevantRaces = races.Where(r => IsComplete(r) && IsPointsScoring(r)).ToList();
            foreach(var race in relevantRaces)
            {
                foreach(var competitor in race.Competitors)
                {
                    var englishPlease = EnglishDammit.TryGetValue(competitor.Name, out var correctName) ? correctName : competitor.Name;
                    if(!nameToPoints.ContainsKey(englishPlease))
                    {
                        nameToPoints[englishPlease] = 0.0;
                    }

                    if(!nameToFakePoints.ContainsKey(englishPlease))
                    {
                        nameToFakePoints[englishPlease] = 0.0;
                    }

                    nameToPoints[englishPlease] += CalculateRealPositionPoints(race, competitor.Place);
                    nameToFakePoints[englishPlease] += CalculateFakePositionPoints(race, competitor.Place);
                }
            }

            var realDrivers = new List<F1.RealF1Driver>();
            foreach(F1.F1Driver driver in fakeStandings.Drivers)
            {
                var realPoints = nameToPoints.TryGetValue(driver.FullName, out double points) ? Math.Round(points, 2) : -1;
                var pointsDiff = Math.Round(realPoints == -1 ? 0 : realPoints - driver.Points, 2);
                var realDriver = new F1.RealF1Driver
                {
                    Name = driver.Name,
                    FakePoints = driver.Points,
                    FakePosition = driver.Position,
                    Points = realPoints,
                    PointsDiff = pointsDiff,
                    Nationality = driver.Nationality,
                    Car = driver.Car,
                    PositionsMoved = -1,
                    Position = -1,
                };
                realDrivers.Add(realDriver);
            }

            realDrivers = realDrivers.OrderByDescending(r => r.Points != -1 ? r.Points : r.FakePoints).ToList();
            for(int i = 0; i < realDrivers.Count; i++)
            {
                var realPosition = i + 1;
                realDrivers[i].Position = realPosition;
                realDrivers[i].PositionsMoved = realDrivers[i].FakePosition - realPosition;
            }

            return new F1RealDriverStandings { Drivers = realDrivers };
        }

        private async Task UpdateScoreBoard(CancellationToken token)
        {
            var boardTasks = RaceDates
                .Select(date => FetchScoreBoard(token, date))
                .ToArray();

            await Task.WhenAll(boardTasks).ContinueWith(bts =>
            {
                if (bts.Status != TaskStatus.RanToCompletion)
                {
                    return;
                }

                var events = bts.Result
                    .SelectMany(sb => sb
                        ?.Sports?.First()
                        ?.Leagues?.First()
                        ?.Events ?? new List<Event>())
                    .ToList();

                // If we have some events stored from the last pull, but did not get any back
                // from the latest pull, we don't want to override that data
                _currentEvents = _currentEvents.Any()
                    ? events.Any() ? events : _currentEvents
                    : events;

            }, token);
        }

        private async Task<F1ScoreBoard> FetchScoreBoard(CancellationToken token, string date)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var uri = new Uri($"https://site.web.api.espn.com/apis/v2/scoreboard/header?sport=racing&league=f1&region=us&lang=en&contentorigin=espn&buyWindow=1m&showAirings=buy,live,replay&showZipLookup=true&tz=America/New_York&dates={date}");
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        var response = await httpClient.SendAsync(request, token);
                        var responseString = await response.Content.ReadAsStringAsync();
                        var bracket = F1ScoreBoard.FromJson(responseString);
                        return bracket;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    
        private async Task<F1.F1TeamStandings> FetchTeamStandings(CancellationToken token)
        {
            try
            {
                var uri = new Uri("https://www.formula1.com/en/results/2025/team");
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {                    
                    var response = await httpClient.SendAsync(request, token);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var standings = ParseF1TeamStandings(responseString);
                    return standings;
                }
            }
            catch
            {
                return new F1TeamStandings();
            }
        }

        private F1.F1TeamStandings ParseF1TeamStandings(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<F1.F1Team> teams = doc.DocumentNode
                .DescendantsWithClass("f1-table-with-data").Single()
                .DescendantsOfType("tbody").Single()
                .DescendantsOfType("tr")
                .Select(ParseF1Team)
                .Where(t => t != null)
                .ToList();

            return new F1TeamStandings { Teams = teams };
        }

        private F1.F1Team ParseF1Team(HtmlNode teamNode)
        {
            try
            {
                var tds = teamNode.DescendantsOfType("td").ToList();

                int position = int.Parse(tds[0].InnerText);
                string team = tds[1].DescendantsOfType("a").Single().InnerText;
                int points = int.Parse(tds[2].InnerText);

                return new F1.F1Team { Name = team, Points = points, Position = position };
            }
            catch (Exception)
            {
                return null;
            }            
        }

        private async Task<F1.F1DriverStandings> FetchDriverStandings(CancellationToken token)
        {
            try
            {
                var uri = new Uri("https://www.formula1.com/en/results/2025/drivers");
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {                    
                    var response = await httpClient.SendAsync(request, token);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var standings = ParseF1DriverStandings(responseString);
                    return standings;
                }
            }
            catch
            {
                return new F1DriverStandings();
            }
        }

        private F1.F1DriverStandings ParseF1DriverStandings(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<F1.F1Driver> drivers = doc.DocumentNode
                .DescendantsWithClass("f1-table-with-data").Single()
                .DescendantsOfType("tbody").Single()
                .DescendantsOfType("tr")
                .Select(ParseF1Driver)
                .Where(t => t != null)
                .ToList();

            return new F1DriverStandings { Drivers = drivers };
        }

        private F1.F1Driver ParseF1Driver(HtmlNode driverNode)
        {
            try
            {
                var tds = driverNode.DescendantsOfType("td").ToList();

                int position = int.Parse(tds[0].InnerText);
                string name = tds[1]
                    .DescendantsOfType("a").Single()
                    .DescendantsWithClass("tablet:hidden").Single()
                    .InnerText;
                string fullName = tds[1].InnerText.Substring(0, tds[1].InnerText.Length - 3).Replace("&nbsp"," ");
                string cleanFullName = Regex.Replace(fullName, "[\u00A0]", " ");
                string nationality = tds[2].InnerText;
                string car = tds[3].DescendantsOfType("a").Single().InnerText;
                int points = int.Parse(tds[4].InnerText);

                return new F1.F1Driver 
                { 
                    Name = name, 
                    FullName = cleanFullName,
                    Points = points, 
                    Position = position,
                    Car = car,
                    Nationality = nationality
                };
            }
            catch (Exception)
            {
                return null;
            }    
        }
    }
}