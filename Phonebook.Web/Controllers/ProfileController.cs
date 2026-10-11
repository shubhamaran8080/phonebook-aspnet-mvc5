using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Phonebook.Web.Models.Entities;
using Phonebook.Web.Models.ViewModels;
using Phonebook.Web.Services;

namespace Phonebook.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private ApplicationUserManager _userManager;
        private AuthService _authService;

        public ProfileController()
        {
        }

        public ProfileController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            private set => _userManager = value;
        }

        private AuthService AuthService
        {
            get
            {
                if (_authService == null)
                    _authService = new AuthService(UserManager, null);
                return _authService;
            }
        }

        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var profile = await AuthService.GetProfileAsync(userId);

            if (profile == null)
                return HttpNotFound();

            return View(profile);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
