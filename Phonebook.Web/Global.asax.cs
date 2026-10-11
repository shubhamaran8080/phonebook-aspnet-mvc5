using System;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Phonebook.Web.Data;

namespace Phonebook.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Initialize database (create if not exists + seed).
            // The DbContext constructor sets its own initializer to null, so we
            // register the real initializer here before the first use of the context.
            Database.SetInitializer(new PhonebookDbInitializer());
            using (var context = new PhonebookDbContext())
            {
                context.Database.Initialize(force: false);
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var lastError = Server.GetLastError();

            if (lastError is HttpException httpException && httpException.GetHttpCode() == 404)
            {
                Response.Clear();
                Response.Redirect("~/Error/NotFound");
            }
        }
    }
}
