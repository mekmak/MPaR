using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
            EspnScoreConnector.Sport sportType;
            if (!Enum.TryParse(gameType.ToLower(), out sportType))
            {
                sportType = EspnScoreConnector.Sport.nfl;
            }

            List<Game> games = EspnScoreConnector.Instance.GetGames(sportType);
            return PartialView("_Games", games);
        }
    }
}