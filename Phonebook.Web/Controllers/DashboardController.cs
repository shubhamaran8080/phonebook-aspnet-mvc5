using System.Threading.Tasks;
using System.Web.Mvc;
using Phonebook.Web.Data;
using Phonebook.Web.Services;

namespace Phonebook.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly PhonebookDbContext _db;
        private readonly StatsService _statsService;

        public DashboardController()
        {
            _db = new PhonebookDbContext();
            _statsService = new StatsService(_db);
        }

        public DashboardController(PhonebookDbContext db)
        {
            _db = db;
            _statsService = new StatsService(_db);
        }

        public async Task<ActionResult> Index()
        {
            var stats = await _statsService.GetStatsAsync(User.Identity.Name);
            return View(stats);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
