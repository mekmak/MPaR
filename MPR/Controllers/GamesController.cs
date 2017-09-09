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

        public ActionResult Games()
        {
            List<Game> games = EspnScoreConnector.Instance.GetGames(EspnScoreConnector.Sport.nfl);
            return PartialView("_Games", games);
        }
    }
}