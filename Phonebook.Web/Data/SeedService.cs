using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Phonebook.Web.Models.Entities;

namespace Phonebook.Web.Data
{
    /// <summary>
    /// Development-only seed utility for generating large synthetic contact datasets.
    /// Idempotent — never overwrites existing contacts.
    /// </summary>
    public class SeedService
    {
        private readonly PhonebookDbContext _db;

        public SeedService(PhonebookDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Generates synthetic contacts. Development-only.
        /// </summary>
        public SeedResult GenerateContacts(int count)
        {
            var before = _db.Contacts.Count();

            var usedPhones = new HashSet<string>(
                _db.Contacts.Select(c => c.PhoneNumber).ToList(),
                StringComparer.OrdinalIgnoreCase);
            var usedEmails = new HashSet<string>(
                _db.Contacts.Where(c => c.Email != null).Select(c => c.Email).ToList(),
                StringComparer.OrdinalIgnoreCase);

            var random = new Random();
            var batch = new List<Contact>();
            var created = 0;
            var attempts = 0;
            const int batchSize = 500;
            const int maxAttemptsPerRecord = 100;

            while (created < count)
            {
                if (attempts++ > count * maxAttemptsPerRecord)
                    throw new InvalidOperationException(
                        "Unable to generate unique contacts after maximum attempts.");

                var phone = RandomPhone(random);
                if (!usedPhones.Add(phone))
                    continue;

                var email = RandomEmail(random);
                if (!usedEmails.Add(email))
                {
                    usedPhones.Remove(phone);
                    continue;
                }

                batch.Add(new Contact
                {
                    Name = RandomName(random),
                    PhoneNumber = phone,
                    Email = email,
                    Address = RandomAddress(random),
                    CreatedAt = DateTime.UtcNow
                });
                created++;

                if (batch.Count < batchSize)
                    continue;

                _db.Contacts.AddRange(batch);
                _db.SaveChanges();
                batch.Clear();
            }

            if (batch.Count > 0)
            {
                _db.Contacts.AddRange(batch);
                _db.SaveChanges();
            }

            var after = _db.Contacts.Count();

            return new SeedResult
            {
                Created = created,
                Before = before,
                After = after
            };
        }

        private static string RandomName(Random random)
        {
            var first = FirstNames[random.Next(FirstNames.Length)];
            var last = LastNames[random.Next(LastNames.Length)];
            return first + " " + last;
        }

        private static string RandomPhone(Random random)
        {
            return string.Format("+1{0:D3}{1:D3}{2:D4}",
                random.Next(200, 1000),
                random.Next(200, 1000),
                random.Next(0, 10000));
        }

        private static string RandomEmail(Random random)
        {
            var first = FirstNames[random.Next(FirstNames.Length)].ToLowerInvariant();
            var last = LastNames[random.Next(LastNames.Length)].ToLowerInvariant();
            var domain = EmailDomains[random.Next(EmailDomains.Length)];
            return string.Format("{0}.{1}{2:D3}@{3}", first, last, random.Next(1, 1000), domain);
        }

        private static string RandomAddress(Random random)
        {
            var number = random.Next(1, 9999);
            var street = StreetNames[random.Next(StreetNames.Length)];
            var city = Cities[random.Next(Cities.Length)];

            var unit = random.Next(3);
            string unitText;
            if (unit == 0)
                unitText = string.Format(", Apt {0}", random.Next(1, 99));
            else if (unit == 1)
                unitText = string.Format(", Unit {0}", random.Next(1, 400));
            else
                unitText = string.Empty;

            return string.Format("{0} {1}{2}, {3}", number, street, unitText, city);
        }

        public class SeedResult
        {
            public int Created { get; set; }
            public int Before { get; set; }
            public int After { get; set; }
        }

        private static readonly string[] FirstNames =
        {
            "Aarav", "Abigail", "Aiden", "Aisha", "Alejandro", "Alicia", "Amara", "Amelia",
            "Ananya", "Andre", "Anthony", "Aria", "Arjun", "Asha", "Ava", "Beatriz",
            "Bilal", "Brandon", "Camille", "Carlos", "Cecilia", "Chloe", "Clara", "Daniel",
            "Darius", "David", "Diego", "Elena", "Elias", "Emeka", "Emma", "Ethan",
            "Farrah", "Fatima", "Felix", "Gabriel", "Gabriela", "Grace", "Hana", "Hassan",
            "Henry", "Ines", "Isaac", "Isabella", "Ivan", "Jamal", "Jasmine", "Javier",
            "Jonas", "Julia", "Kaito", "Karim", "Kavya", "Kevin", "Kofi", "Laila",
            "Layla", "Leah", "Liam", "Linnea", "Lucas", "Lucia", "Luis", "Marcus",
            "Maria", "Mateo", "Maya", "Meera", "Miguel", "Mila", "Nadia", "Nathan",
            "Nia", "Nikhil", "Noor", "Oliver", "Omar", "Oscar", "Priya", "Quinn",
            "Rafael", "Rania", "Rosa", "Ruth", "Samir", "Samuel", "Sara", "Sebastian",
            "Serena", "Sofia", "Tamara", "Tenzin", "Thomas", "Umar", "Valeria", "Viktor",
            "Vivek", "Wanjiru", "Wei", "Yara", "Yusuf", "Zainab", "Zoe", "Zoey"
        };

        private static readonly string[] LastNames =
        {
            "Abiodun", "Adeyemi", "Aguilar", "Ali", "Alvarez", "Anderson", "Andersen", "Arora",
            "Baker", "Barnes", "Bello", "Bennett", "Bianchi", "Blanco", "Boateng", "Brooks",
            "Cabrera", "Castillo", "Chandra", "Chatterjee", "Chen", "Clark", "Cohen", "Costa",
            "Dabrowski", "Dahl", "Dalton", "Devi", "Diaz", "Dubois", "Duarte", "Ebadi",
            "El-Sayed", "Eze", "Farah", "Fernandez", "Fischer", "Fitzgerald", "Fleming", "Gallo",
            "Garcia", "Ghosh", "Gomez", "Gonzalez", "Grant", "Gullit", "Gupta", "Haddad",
            "Hansen", "Hassan", "Hernandez", "Hoffman", "Ibrahim", "Iyer", "Janssen", "Jimenez",
            "Kabir", "Karlsson", "Keller", "Khan", "Kim", "Kowalski", "Kruger", "Kumar",
            "Larsen", "Laurent", "Lawal", "Lebedev", "Lindqvist", "Lopez", "Lund", "Mahmoud",
            "Malhotra", "Marchetti", "Martins", "Mbeki", "Mendes", "Meyer", "Mikaelsen", "Mitchell",
            "Morales", "Moreau", "Moser", "Mwangi", "Nakamura", "Navarro", "Nguyen", "Nielsen",
            "Novak", "Okafor", "Olawale", "Olsen", "Osei", "Park", "Patel", "Pavlov",
            "Pettersen", "Pham", "Popov", "Prasad", "Quintero", "Rahman", "Ramirez", "Ramos",
            "Reyes", "Rossi", "Rowe", "Ruiz", "Sanchez", "Santos", "Sato", "Schmidt",
            "Sharma", "Silva", "Singh", "Sokolov", "Sorensen", "Suzuki", "Tanaka", "Tavares",
            "Torres", "Tran", "Vargas", "Vasquez", "Vogel", "Wang", "Weber", "Yilmaz",
            "Yoon", "Zamora", "Zhang", "Zhao", "Zheng", "Zhou"
        };

        private static readonly string[] EmailDomains =
        {
            "example.com",
            "example.net",
            "example.org"
        };

        private static readonly string[] StreetNames =
        {
            "Maple Street", "Oak Avenue", "Cedar Lane", "Pine Road", "Elm Drive",
            "Birch Court", "Willow Way", "Spruce Boulevard", "Aspen Circle", "Chestnut Place",
            "Magnolia Street", "Dogwood Avenue", "Poplar Lane", "Sycamore Road", "Walnut Drive",
            "Harborview Drive", "Kingsley Road", "Bridgewater Lane", "Meadowlark Way", "Stonebridge Avenue"
        };

        private static readonly string[] Cities =
        {
            "Springfield, IL", "Riverton, TX", "Lakeview, OH", "Fairview, OR", "Milton, GA",
            "Arlington, VA", "Greenville, SC", "Burlington, VT", "Clinton, IA", "Dover, DE",
            "Franklin, TN", "Georgetown, KY", "Madison, WI", "Oxford, MS", "Salem, MA"
        };
    }
}
