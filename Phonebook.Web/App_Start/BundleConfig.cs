using System.Web;
using System.Web.Optimization;

namespace Phonebook.Web
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // Plain (non-minifying) bundles are used on purpose.
            // ScriptBundle/StyleBundle require the WebGrease minifier, which is not
            // available in this environment; Bundle keeps bundling working without it.
            bundles.Add(new Bundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-3.7.1.js"));

            bundles.Add(new Bundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate.js",
                        "~/Scripts/jquery.validate.unobtrusive.js"));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                        "~/Scripts/bootstrap.min.js"));

            bundles.Add(new Bundle("~/Content/css").Include(
                        "~/Content/site.css"));
        }
    }
}
