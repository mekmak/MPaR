using System;
using System.Collections.Generic;
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
            SportType sportType = GetSportType(gameType);
            switch (sportType)
            {
                case SportType.Espn:
                    var espnGames = GetEspnGames(gameType);
                    return PartialView("_EspnGames", espnGames);
                case SportType.Owl:
                    var owlGames = OwlConnectorV2.Instance.GetGames(offset);
                    return PartialView("_OwlGames", owlGames);
                case SportType.OwlSt:
                    var st = OwlConnectorV2.Instance.GetTournaments();
                    return PartialView("_OwlStandings", st);
                default:
                    return PartialView("_UnknownGame");
            }
        }
        
        private List<EspnGame> GetEspnGames(string gameType)
        {
            EspnScoreConnector.Sport sportType;
            if(!Enum.TryParse(gameType.ToLower(), out sportType))
            {
                sportType = EspnScoreConnector.Sport.nfl;
            }

            List<EspnGame> games = EspnScoreConnector.Instance.GetGames(sportType);
            return games;
        }


        private SportType GetSportType(string gameType)
        {
            if (string.IsNullOrWhiteSpace(gameType))
            {
                return SportType.None;
            }

            if (Enum.IsDefined(typeof(EspnScoreConnector.Sport), gameType))
            {
                return SportType.Espn;
            }

            if (gameType.Equals("owl"))
            {
                return SportType.Owl;
            }

            if (gameType.Equals("owlSt"))
            {
                return SportType.OwlSt;
            }

            return SportType.None;
        }


        private enum SportType
        {
            None,
            Espn,
            Owl,
            OwlSt
        }
    }
}