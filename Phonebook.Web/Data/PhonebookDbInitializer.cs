using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Configuration;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Phonebook.Web.Models.Entities;
using Phonebook.Web.Services;

namespace Phonebook.Web.Data
{
    /// <summary>
    /// Database initializer that creates the schema and seeds demo data.
    /// Safe and idempotent — never drops or overwrites existing data.
    /// Handles three cases:
    /// 1. Database missing -> created with schema, then seeded.
    /// 2. Database exists but schema is missing (e.g. an empty database) ->
    ///    schema is created from the model, then seeded.
    /// 3. Database and schema exist -> left untouched.
    /// </summary>
    public class PhonebookDbInitializer : CreateDatabaseIfNotExists<PhonebookDbContext>
    {
        public override void InitializeDatabase(PhonebookDbContext context)
        {
            if (!context.Database.Exists())
            {
                // Case 1: create the database, schema and seed data.
                base.InitializeDatabase(context);
                return;
            }

            // Case 2: database exists. If the schema is missing, create it
            // from the model. CreateDatabaseScript only emits CREATE
            // statements, so existing data is never dropped or modified.
            if (!SchemaExists(context))
            {
                var objectContext = ((IObjectContextAdapter)context).ObjectContext;
                string createScript = objectContext.CreateDatabaseScript();
                context.Database.ExecuteSqlCommand(createScript);
                Seed(context);
            }
        }

        private static bool SchemaExists(PhonebookDbContext context)
        {
            const string sql =
                "SELECT COUNT(*) FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo')";
            return context.Database.SqlQuery<int>(sql).Single() > 0;
        }
        protected override void Seed(PhonebookDbContext context)
        {
            var createDemoUser = WebConfigurationManager.AppSettings["DemoUserEnabled"];
            if (createDemoUser?.ToLower() != "true")
                return;

            var username = WebConfigurationManager.AppSettings["DemoUserUsername"] ?? "demo";
            var email = WebConfigurationManager.AppSettings["DemoUserEmail"] ?? "demo@example.com";
            var password = WebConfigurationManager.AppSettings["DemoUserPassword"] ?? "Password123!";

            // Create demo user if not exists
            // Use ApplicationUserManager so the password is hashed
            // with BCrypt (matching login verification).
            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new ApplicationUserManager(userStore);

            if (!context.Users.Any(u => u.UserName == username))
            {
                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };
                userManager.Create(user, password);
            }

            // Seed sample contacts if none exist
            if (!context.Contacts.Any())
            {
                var sampleContacts = new[]
                {
                    new Contact { Name = "Rahul Sharma", PhoneNumber = "+919876543210", Email = "rahul@example.com", Address = "Pune" },
                    new Contact { Name = "Amit Patil", PhoneNumber = "+919876543211", Email = "amit@example.com", Address = "Mumbai" },
                    new Contact { Name = "Priya Singh", PhoneNumber = "+919876543212", Email = "priya@example.com", Address = "Delhi" },
                    new Contact { Name = "Sneha Gupta", PhoneNumber = "+919876543213", Email = "sneha@example.com", Address = "Bangalore" },
                    new Contact { Name = "Vikram Rao", PhoneNumber = "+919876543214", Email = "vikram@example.com", Address = "Hyderabad" },
                    new Contact { Name = "Ananya Iyer", PhoneNumber = "+919876543215", Email = "ananya@example.com", Address = "Chennai" },
                    new Contact { Name = "Karan Mehta", PhoneNumber = "+919876543216", Email = "karan@example.com", Address = "Kolkata" },
                    new Contact { Name = "Divya Nair", PhoneNumber = "+919876543217", Email = "divya@example.com", Address = "Kochi" },
                    new Contact { Name = "Arjun Desai", PhoneNumber = "+919876543218", Email = "arjun@example.com", Address = "Ahmedabad" },
                    new Contact { Name = "Meera Joshi", PhoneNumber = "+919876543219", Email = "meera@example.com", Address = "Jaipur" }
                };
                context.Contacts.AddRange(sampleContacts);
            }

            context.SaveChanges();
        }
    }
}
