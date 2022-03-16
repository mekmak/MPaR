using System.Threading;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MPR.ScoreConnectors;

namespace MPR
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            EspnScoreConnector.Instance.InitGameDownload();
            OwlConnectorV2.Instance.InitGameDownload(_tokenSource.Token);
            NcaaScoreConnector.Instance.Init(_tokenSource.Token);
        }

        protected void Application_End()
        {
            _tokenSource.Cancel();
        }
    }
}
