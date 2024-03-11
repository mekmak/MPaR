using System.Threading;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MPR.Connectors;

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

            MeatSportsConnector.Instance.Init(_tokenSource.Token);
            //OwlConnector.Instance.Init(_tokenSource.Token);
            F1Connector.Instance.Init(_tokenSource.Token);
            //NcaaConnector.Instance.Init(_tokenSource.Token);
        }

        protected void Application_End()
        {
            _tokenSource.Cancel();
        }
    }
}
