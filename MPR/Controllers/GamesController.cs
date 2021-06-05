using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MPR.Models;
using MPR.ScoreConnectors;

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
                    var meatSports = GetMeatSports();
                    return PartialView("_EspnGames", meatSports);
                case "owl":
                    var owlGames = OwlConnectorV2.Instance.GetGames(offset);
                    return PartialView("_OwlGames", owlGames);
                case "owlSt":
                    var st = OwlConnectorV2.Instance.GetTournaments();
                    return PartialView("_OwlStandings", st);
                default:
                    return PartialView("_UnknownGame");
            }
        }

        private MeatSports GetMeatSports()
        {
            var sports = new List<MeatSport>();
            foreach(EspnScoreConnector.Sport s in Enum.GetValues(typeof(EspnScoreConnector.Sport)))
            {
                var games = EspnScoreConnector.Instance.GetGames(s);
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