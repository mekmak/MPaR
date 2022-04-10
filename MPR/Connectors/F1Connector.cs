using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MPR.F1;
using MPR.Models;
using Event = MPR.F1.Event;

namespace MPR.Connectors
{
    public class F1Connector : Connector
    {
        public static F1Connector Instance = new F1Connector();
        private List<Event> _currentEvents = new List<Event>();

        public void Init(CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var pulls = new[]
            {
                new Pull
                {
                    Name = "F1 Schedule", Task = UpdateScoreBoard
                }
            };

            StartPulls(token, pulls);
        }

        public Models.F1Schedule GetSchedule(int clientOffset)
        {
            var races = new Dictionary<string, Models.Race>();

            int counter = 1;
            foreach (var e in _currentEvents.OrderBy(e => e.DateUtc))
            {
                if (!races.TryGetValue(e.Name, out var race))
                {
                    race = new Race
                    {
                        Order = counter++,
                        Name = e.Name,
                        Events = new List<Models.Event>(),
                        Link = e.Link
                    };

                    races[e.Name] = race;
                }

                race.Events.Add(Wrap(e, clientOffset));
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
            "20220320", // Bahrain
            "20220325", // Saudi
            "20220408", // Australian
            "20220422", // Emilia Romagna
            "20220506", // Miami
            "20220520", // Spanish
            "20220527", // Monaco
            "20220610", // Azerbaijan
            "20220617", // Canadian
            "20220701", // British
            "20220708", // Austrian
            "20220722", // French
            "20220729", // Hungarian
            "20220826", // Belgian 
            "20220902", // Dutch
            "20220909", // Italian
            "20220923", // Russian
            "20220930", // Singapore
            "20221007", // Japanese
            "20221021", // United States
            "20221028", // Mexico City
            "20221111", // Sao Paulo
            "20221118", // Abu Dhabi
        };

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
                        ?.Sports.First()
                        ?.Leagues.First()
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
    }
}