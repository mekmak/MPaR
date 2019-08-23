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

        public ActionResult Games(string gameType)
        {
            List<Game> games = GetGames(gameType);
            return PartialView("_Games", games);
        }

        private List<Game> GetGames(string gameType)
        {
            SportType sportType = GetSportType(gameType);

            switch (sportType)
            {
                case SportType.Espn:
                    return GetEspnGames(gameType);
                case SportType.Owl:
                    return GetOwlGames();
                default:
                    return GetEspnGames(EspnScoreConnector.Sport.nfl.ToString());
            }
        }

        private List<Game> GetOwlGames()
        {
            return OwlConnector.Instance.GetGames();
        }

        private List<Game> GetEspnGames(string gameType)
        {
            EspnScoreConnector.Sport sportType;
            if(!Enum.TryParse(gameType.ToLower(), out sportType))
            {
                sportType = EspnScoreConnector.Sport.nfl;
            }

            List<Game> games = EspnScoreConnector.Instance.GetGames(sportType);
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