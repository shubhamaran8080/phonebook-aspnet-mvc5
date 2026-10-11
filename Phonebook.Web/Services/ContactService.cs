using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Phonebook.Web.Data;
using Phonebook.Web.Models.Entities;
using Phonebook.Web.Models.ViewModels;

namespace Phonebook.Web.Services
{
    /// <summary>
    /// Contact CRUD operations with search and pagination.
    /// </summary>
    public class ContactService
    {
        private readonly PhonebookDbContext _db;

        public ContactService(PhonebookDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Gets a contact by ID or returns null.
        /// </summary>
        public async Task<Contact> GetContactAsync(int id)
        {
            return await _db.Contacts.FindAsync(id);
        }

        /// <summary>
        /// Creates a new contact. Throws on duplicate phone/email.
        /// </summary>
        public async Task<Contact> CreateAsync(ContactFormViewModel model)
        {
            var contact = new Contact
            {
                Name = model.Name.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim()
            };

            await EnsureUniqueAsync(contact.Id, contact.PhoneNumber, contact.Email);

            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();

            return contact;
        }

        /// <summary>
        /// Updates a contact.
        /// </summary>
        public async Task<Contact> UpdateAsync(int id, ContactFormViewModel model)
        {
            var contact = await GetContactAsync(id);
            if (contact == null) return null;

            contact.Name = model.Name.Trim();
            contact.PhoneNumber = model.PhoneNumber.Trim();
            contact.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            contact.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

            await EnsureUniqueAsync(contact.Id, contact.PhoneNumber, contact.Email);

            await _db.SaveChangesAsync();

            return contact;
        }

        /// <summary>
        /// Deletes a contact by ID.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var contact = await GetContactAsync(id);
            if (contact == null) return false;

            _db.Contacts.Remove(contact);
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Searches contacts by name or phone number with pagination.
        /// </summary>
        public async Task<ContactListViewModel> SearchAsync(string query, int page, int pageSize)
        {
            var contactsQuery = _db.Contacts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var needle = query.Trim().ToLowerInvariant();
                contactsQuery = contactsQuery.Where(c => c.Name.ToLower().Contains(needle)
                    || c.PhoneNumber.ToLower().Contains(needle));
            }

            var totalCount = await contactsQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

            // Clamp page
            page = Math.Max(1, Math.Min(page, totalPages));

            var items = await contactsQuery
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ContactListViewModel
            {
                Contacts = items.Select(c => new ContactViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    Address = c.Address,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchQuery = query,
                RangeStart = totalCount == 0 ? 0 : (page - 1) * pageSize + 1,
                RangeEnd = Math.Min(totalCount, page * pageSize)
            };
        }

        /// <summary>
        /// Checks for duplicate phone/email.
        /// </summary>
        private async Task EnsureUniqueAsync(int id, string phoneNumber, string email)
        {
            var duplicatePhone = await _db.Contacts
                .AnyAsync(c => c.Id != id && c.PhoneNumber == phoneNumber);

            if (duplicatePhone)
                throw new InvalidOperationException("A contact with this phone number or email already exists.");

            if (string.IsNullOrWhiteSpace(email))
                return;

            var duplicateEmail = await _db.Contacts
                .AnyAsync(c => c.Id != id && c.Email == email);

            if (duplicateEmail)
                throw new InvalidOperationException("A contact with this phone number or email already exists.");
        }
    }
}
