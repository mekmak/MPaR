using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MPR.Connectors;
using MPR.Models;

namespace MPR.Controllers
{
    public class GamesController : Controller
    {
        // GET: Games
        public ActionResult Index()
        {
            return View();
        }        

        public ActionResult Games(string gameType, int offset = 0)
        {
            switch(gameType)
            {
                case "meat":
                    MeatSports meatSports = GetMeatSports();
                    return PartialView("_MeatSportsGames", meatSports);
                case "owl":
                    List<OwlGame> owlGames = OwlConnector.Instance.GetGames(offset);
                    return PartialView("_OwlGames", owlGames);
                case "owlSt":
                    List<Standings> st = OwlConnector.Instance.GetStandings();
                    return PartialView("_OwlStandings", st);
                case "ncaa":
                    NcaaBracket b = NcaaConnector.Instance.GetBracket(offset);
                    return PartialView("_NcaaGames", b);
                case "f1":
                    F1Schedule f1s = F1Connector.Instance.GetSchedule(offset);
                    return PartialView("_F1Schedule", f1s);
                case "f1St":
                    var f1Standings = F1Connector.Instance.GetStandings();
                    return PartialView("_F1Standings", f1Standings);
                default:
                    return PartialView("_UnknownGame");
            }
        }

        private MeatSports GetMeatSports()
        {
            var sports = new List<MeatSport>();
            foreach(MeatSportsConnector.Sport s in Enum.GetValues(typeof(MeatSportsConnector.Sport)))
            {
                var games = MeatSportsConnector.Instance.GetGames(s);
                if(!games.Any())
                {
                    continue;
                }

                sports.Add(new MeatSport { Name = s.ToString(), Games = games });
            }

            return new MeatSports { Sports = sports };
        }
    }
}