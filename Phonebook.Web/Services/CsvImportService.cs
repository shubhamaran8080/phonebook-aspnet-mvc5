using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CsvHelper;
using CsvHelper.Configuration;
using Phonebook.Web.Data;
using Phonebook.Web.Models.Entities;

namespace Phonebook.Web.Services
{
    /// <summary>
    /// CSV import: .csv only, max 5 MB / 5000 rows, UTF-8 with BOM tolerance,
    /// per-row validation, duplicate detection.
    /// </summary>
    public class CsvImportService
    {
        public const int MaxBytes = 5 * 1024 * 1024; // 5 MB
        public const int MaxRows = 5000;

        private static readonly string[] CanonicalColumns = { "name", "phone_number", "email", "address" };

        private readonly PhonebookDbContext _db;

        public CsvImportService(PhonebookDbContext db)
        {
            _db = db;
        }

        public class CsvImportResult
        {
            public int Total { get; set; }
            public int Imported { get; set; }
            public int Failed { get; set; }
            public int Duplicates { get; set; }
            public List<CsvRowError> Errors { get; set; } = new List<CsvRowError>();
        }

        public class CsvRowError
        {
            public int Row { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        /// <summary>
        /// Imports contacts from an uploaded CSV file.
        /// </summary>
        public async Task<CsvImportResult> ImportAsync(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                throw new ArgumentException("Missing required part: file");

            if (string.IsNullOrEmpty(file.FileName) || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only .csv files can be imported.");

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await file.InputStream.CopyToAsync(ms);
                content = ms.ToArray();
            }

            if (content.Length > MaxBytes)
                throw new ArgumentException("The CSV file is too large (maximum is 5 MB).");

            var text = DecodeUtf8(content);
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("The CSV file is empty.");

            var validRows = new List<Dictionary<string, string>>();
            var errors = new List<CsvRowError>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            using (var reader = new StringReader(text))
            using (var csv = new CsvReader(reader, config))
            {
                await csv.ReadAsync();
                csv.ReadHeader();

                var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var headerRecord = csv.Context.HeaderRecord;
                if (headerRecord != null)
                {
                    foreach (var header in headerRecord)
                    {
                        if (header == null) continue;
                        var canonical = header.Trim().ToLowerInvariant();
                        if (CanonicalColumns.Contains(canonical) && !headerMap.ContainsKey(canonical))
                            headerMap[canonical] = header;
                    }
                }

                if (!headerMap.ContainsKey("name") || !headerMap.ContainsKey("phone_number"))
                    throw new ArgumentException("The CSV must include 'name' and 'phone_number' columns.");

                var rowNumber = 1; // header is row 1; first data row is 2
                while (await csv.ReadAsync())
                {
                    rowNumber++;

                    if (validRows.Count + errors.Count >= MaxRows)
                        throw new ArgumentException("The CSV file has too many rows (maximum is 5000).");

                    var payload = new Dictionary<string, string>();
                    foreach (var col in CanonicalColumns)
                    {
                        payload[col] = null;
                    }

                    foreach (var col in headerMap)
                    {
                        var value = csv.GetField(col.Value);
                        value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                        payload[col.Key] = value;
                    }

                    var rowErrors = ValidateRow(payload);
                    if (rowErrors.Count > 0)
                    {
                        errors.Add(new CsvRowError { Row = rowNumber, Errors = rowErrors });
                        continue;
                    }

                    validRows.Add(payload);
                }
            }

            // Duplicate detection: existing phones/emails + within-file
            var existingPhones = new HashSet<string>(
                await _db.Contacts.Select(c => c.PhoneNumber).ToListAsync());
            var existingEmails = new HashSet<string>(
                await _db.Contacts.Where(c => c.Email != null).Select(c => c.Email).ToListAsync());

            var toInsert = new List<Contact>();
            var imported = 0;
            var duplicates = 0;

            foreach (var payload in validRows)
            {
                var phone = payload["phone_number"];
                var email = payload["email"];

                if (existingPhones.Contains(phone) || (!string.IsNullOrEmpty(email) && existingEmails.Contains(email)))
                {
                    duplicates++;
                    continue;
                }

                toInsert.Add(new Contact
                {
                    Name = payload["name"],
                    PhoneNumber = phone,
                    Email = email,
                    Address = payload["address"]
                });

                existingPhones.Add(phone);
                if (!string.IsNullOrEmpty(email)) existingEmails.Add(email);
                imported++;
            }

            if (toInsert.Count > 0)
            {
                _db.Contacts.AddRange(toInsert);
                await _db.SaveChangesAsync();
            }

            return new CsvImportResult
            {
                Total = validRows.Count + errors.Count,
                Imported = imported,
                Failed = errors.Count,
                Duplicates = duplicates,
                Errors = errors
            };
        }

        private static List<string> ValidateRow(Dictionary<string, string> payload)
        {
            var errors = new List<string>();

            var name = payload.ContainsKey("name") ? payload["name"] : null;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
                errors.Add("name: is required");

            var phone = payload.ContainsKey("phone_number") ? payload["phone_number"] : null;
            if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+[1-9]\d{1,14}$"))
                errors.Add("phone_number: must match E.164 format");

            var email = payload.ContainsKey("email") ? payload["email"] : null;
            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add("email: must be a valid email address");

            return errors;
        }

        private static string DecodeUtf8(byte[] content)
        {
            var offset = 0;
            if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
                offset = 3;

            try
            {
                var strictUtf8 = new UTF8Encoding(false, true);
                return strictUtf8.GetString(content, offset, content.Length - offset);
            }
            catch (DecoderFallbackException)
            {
                throw new ArgumentException("The CSV file must be UTF-8 encoded.");
            }
        }
    }
}
