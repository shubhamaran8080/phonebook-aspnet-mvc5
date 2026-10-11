using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Phonebook.Web.Data;
using Phonebook.Web.Models.Entities;
using Phonebook.Web.Models.ViewModels;
using Phonebook.Web.Services;

namespace Phonebook.Web.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly PhonebookDbContext _db;
        private readonly ContactService _contactService;
        private readonly CsvImportService _csvImportService;
        private readonly CsvExportService _csvExportService;
        private readonly SeedService _seedService;

        public ContactsController()
        {
            _db = new PhonebookDbContext();
            _contactService = new ContactService(_db);
            _csvImportService = new CsvImportService(_db);
            _csvExportService = new CsvExportService(_db);
            _seedService = new SeedService(_db);
        }

        public ContactsController(PhonebookDbContext db)
        {
            _db = db;
            _contactService = new ContactService(_db);
            _csvImportService = new CsvImportService(_db);
            _csvExportService = new CsvExportService(_db);
            _seedService = new SeedService(_db);
        }

        // GET: Contacts
        public async Task<ActionResult> Index(string q, int? page, int? pageSize)
        {
            var currentPageSize = pageSize ?? 10;
            currentPageSize = Math.Max(1, Math.Min(currentPageSize, 100));
            var currentPage = Math.Max(1, page ?? 1);

            var result = await _contactService.SearchAsync(q, currentPage, currentPageSize);
            return View(result);
        }

        // GET: Contacts/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var contact = await _contactService.GetContactAsync(id.Value);
            if (contact == null)
                return HttpNotFound();

            var viewModel = new ContactFormViewModel
            {
                Id = contact.Id,
                Name = contact.Name,
                PhoneNumber = contact.PhoneNumber,
                Email = contact.Email,
                Address = contact.Address,
                CreatedAt = contact.CreatedAt
            };

            return View(viewModel);
        }

        // GET: Contacts/Create
        public ActionResult Create()
        {
            return View(new ContactFormViewModel());
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var contact = await _contactService.CreateAsync(model);
                TempData["SuccessMessage"] = "Contact created successfully.";
                return RedirectToAction("Details", new { id = contact.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // GET: Contacts/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var contact = await _contactService.GetContactAsync(id.Value);
            if (contact == null)
                return HttpNotFound();

            var viewModel = new ContactFormViewModel
            {
                Id = contact.Id,
                Name = contact.Name,
                PhoneNumber = contact.PhoneNumber,
                Email = contact.Email,
                Address = contact.Address,
                CreatedAt = contact.CreatedAt
            };

            return View(viewModel);
        }

        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var contact = await _contactService.UpdateAsync(model.Id, model);
                if (contact == null)
                    return HttpNotFound();

                TempData["SuccessMessage"] = "Contact updated successfully.";
                return RedirectToAction("Details", new { id = contact.Id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // POST: Contacts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _contactService.DeleteAsync(id);
            if (!success)
                return HttpNotFound();

            TempData["SuccessMessage"] = "Contact deleted successfully.";
            return RedirectToAction("Index");
        }

        // POST: Contacts/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Import(HttpPostedFileBase file)
        {
            try
            {
                var result = await _csvImportService.ImportAsync(file);
                TempData["ImportResult"] = result;
                TempData["SuccessMessage"] = string.Format(
                    "Import complete — {0} imported, {1} duplicates skipped, {2} failed.",
                    result.Imported, result.Duplicates, result.Failed);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Contacts/Export
        public async Task<ActionResult> Export(string q)
        {
            var content = await _csvExportService.BuildAsync(q);
            var filename = "phonebook-export-" + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".csv";
            return File(content, "text/csv", filename);
        }

        // POST: Contacts/Seed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Seed(int count)
        {
            if (count <= 0 || count > 10000)
            {
                TempData["ErrorMessage"] = "Seed count must be between 1 and 10,000.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = _seedService.GenerateContacts(count);
                TempData["SuccessMessage"] = string.Format(
                    "Seed complete — {0} contacts created (total: {1}).",
                    result.Created, result.After);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Seed failed: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
