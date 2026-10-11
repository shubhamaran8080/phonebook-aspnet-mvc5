using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Phonebook.Web.Data;
using Phonebook.Web.Models.Entities;

namespace Phonebook.Web.Services
{
    /// <summary>
    /// CSV export with formula-injection protection.
    /// </summary>
    public class CsvExportService
    {
        private readonly PhonebookDbContext _db;

        public CsvExportService(PhonebookDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Builds the CSV document containing every contact the search matches.
        /// </summary>
        public async Task<byte[]> BuildAsync(string search)
        {
            var query = _db.Contacts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var needle = search.Trim().ToLowerInvariant();
                query = query.Where(c => c.Name.ToLower().Contains(needle)
                    || c.PhoneNumber.ToLower().Contains(needle));
            }

            var contacts = await query.OrderBy(c => c.Id).ToListAsync();

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                NewLine = CsvHelper.Configuration.NewLine.LF,
                Encoding = new UTF8Encoding(false)
            };

            var buffer = new System.IO.MemoryStream();
            using (var writer = new System.IO.StreamWriter(buffer, Encoding.UTF8, 1024, leaveOpen: true))
            {
                var csv = new CsvWriter(writer, config);

                csv.WriteField("name");
                csv.WriteField("phone_number");
                csv.WriteField("email");
                csv.WriteField("address");
                csv.NextRecord();

                foreach (var contact in contacts)
                {
                    csv.WriteField(Sanitize(contact.Name));
                    csv.WriteField(Sanitize(contact.PhoneNumber));
                    csv.WriteField(Sanitize(contact.Email));
                    csv.WriteField(Sanitize(contact.Address));
                    csv.NextRecord();
                }

                await writer.FlushAsync();
            }

            return buffer.ToArray();
        }

        /// <summary>
        /// Prefixes values that spreadsheet apps could interpret as formulas.
        /// </summary>
        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if ("=+-@".IndexOf(value[0]) >= 0)
                return "'" + value;
            return value;
        }
    }
}
