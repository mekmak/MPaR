using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            var drivers = _currentDriverStandings.Drivers.Select(Wrap).ToList();
            return new Models.F1Standings 
            { 
                TeamStandings = teams,
                DriverStandings = drivers
            };
        }

        private static readonly Dictionary<string,string> TeamShortNames = new Dictionary<string, string>
        {
            {"Red Bull Racing Honda RBPT", "Red Bull"},
            {"Ferrari", "Ferrari"},
            {"Mercedes", "Mercedes"},
            {"McLaren Mercedes", "McLaren"},
            {"Alfa Romeo Ferrari", "Alfa Romeo"},
            {"Alpine Renault", "Alpine"},
            {"AlphaTauri Honda RBPT", "AlphaTauri"},
            {"Haas Ferrari", "Haas"},
            {"Aston Martin Aramco Mercedes", "Aston Martin"},
            {"Williams Mercedes", "Williams"}
        };

        private Models.F1Team Wrap(F1.F1Team team)
        {
            return new Models.F1Team
            {
                Name = team.Name,
                Points = team.Points,
                Position = team.Position,
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

        private static readonly HashSet<string> CompleteRaceSummaries = new HashSet<string> { "Final", "Canceled" };
        private bool IsComplete(Event e)
        {
            return CompleteRaceSummaries.Contains(e.Summary);
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
            "20240301", // Bahrain
            "20240309", // Saudi
            "20240324", // Australian
            "20240407", // Japan
            "20240421", // China
            "20240505", // Miami
            "20240519", // Italy
            "20240526", // Monaco
            "20240609", // Canada
            "20240623", // Spain
            "20240630", // Austria
            "20240707", // Silverstone
            "20240721", // Hungary
            "20240728", // Spa
            "20240825", // Zandvoort
            "20240901", // Monza
            "20240915", // Baku
            "20240922", // Singapore
            "20241020", // Cota
            "20241027", // Mexico
            "20241103", // Brazil
            "20241124", // Las Vegas BABY
            "20241201", // Qatar
            "20241208", // Abu Dhabi
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
                var uri = new Uri("https://www.formula1.com/en/results/jcr:content/resultsarchive.html/2024/team.html");
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
                .DescendantsWithClass("resultsarchive-table").Single()
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

                int position = int.Parse(tds[1].InnerText);
                string team = tds[2].DescendantsOfType("a").Single().InnerText;
                int points = int.Parse(tds[3].InnerText);

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
                var uri = new Uri("https://www.formula1.com/en/results/jcr:content/resultsarchive.html/2024/drivers.html");
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
                .DescendantsWithClass("resultsarchive-table").Single()
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

                int position = int.Parse(tds[1].InnerText);
                string team = tds[2]
                    .DescendantsOfType("a").Single()
                    .DescendantsWithClass("hide-for-desktop").Single()
                    .InnerText;
                string nationality = tds[3].InnerText;
                string car = tds[4].DescendantsOfType("a").Single().InnerText;
                int points = int.Parse(tds[5].InnerText);

                return new F1.F1Driver 
                { 
                    Name = team, 
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