using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Phonebook.Web.Models.Entities;
using Phonebook.Web.Models.ViewModels;

namespace Phonebook.Web.Services
{
    /// <summary>
    /// Authentication logic: login with username or email, cookie-based auth.
    /// </summary>
    public class AuthService
    {
        private readonly ApplicationUserManager _userManager;
        private readonly ApplicationSignInManager _signInManager;

        public AuthService(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Authenticates with a username OR an email.
        /// </summary>
        public async Task<SignInStatus> LoginAsync(string username, string password, bool rememberMe)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                user = await _userManager.FindByEmailAsync(username);

            if (user == null)
                return SignInStatus.Failure;

            var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, shouldLockout: false);
            return result;
        }

        /// <summary>
        /// Signs the user out.
        /// </summary>
        public void Logout()
        {
            HttpContext.Current.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
        }

        /// <summary>
        /// Gets the current user's profile info.
        /// </summary>
        public async Task<ProfileViewModel> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new ProfileViewModel
            {
                Id = user.Id.GetHashCode(),
                Username = user.UserName,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
