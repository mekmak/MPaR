using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MPR.Models.Games;
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
                default:
                    var owlGames = GetOwlGames(offset);
                    return PartialView("_OwlGames", owlGames);

            }
        }

        private List<OwlGame> GetOwlGames(int clientOffset)
        {
            return OwlConnector.Instance.GetGames(clientOffset);
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

            return SportType.None;
        }


        private enum SportType
        {
            None,
            Espn,
            Owl
        }
    }
}