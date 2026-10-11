using System.Web.Mvc;

namespace Phonebook.Web.Controllers
{
    /// <summary>
    /// Handles friendly error pages referenced from Web.config customErrors.
    /// </summary>
    [AllowAnonymous]
    public sealed class ErrorController : Controller
    {
        // GET: Error/NotFound
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        // GET: Error/ServerError
        public ActionResult ServerError()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        // GET: Error
        public ActionResult Index()
        {
            return ServerError();
        }
    }
}
