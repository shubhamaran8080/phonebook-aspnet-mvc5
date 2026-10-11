using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Phonebook.Web.Data;
using Phonebook.Web.Models.Entities;
using Phonebook.Web.Models.ViewModels;

namespace Phonebook.Web.Services
{
    /// <summary>
    /// Dashboard statistics.
    /// </summary>
    public class StatsService
    {
        private readonly PhonebookDbContext _db;

        public StatsService(PhonebookDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns total contacts, contacts added in the last 7 days,
        /// contacts with email, contacts with phone, and the 5 most recent.
        /// </summary>
        public async Task<DashboardViewModel> GetStatsAsync(string username)
        {
            var since = DateTime.UtcNow.AddDays(-7);

            var total = await _db.Contacts.CountAsync();
            var recent = await _db.Contacts.CountAsync(c => c.CreatedAt >= since);
            var withEmail = await _db.Contacts.CountAsync(c => c.Email != null);
            var withPhone = await _db.Contacts.CountAsync(c => c.PhoneNumber != null);

            var recentList = await _db.Contacts
                .AsNoTracking()
                .Where(c => c.CreatedAt >= since)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(5)
                .ToListAsync();

            return new DashboardViewModel
            {
                TotalContacts = total,
                RecentContacts = recent,
                WithEmail = withEmail,
                WithPhone = withPhone,
                RecentList = recentList.Select(c => new ContactViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    Address = c.Address,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                Username = username
            };
        }
    }
}
