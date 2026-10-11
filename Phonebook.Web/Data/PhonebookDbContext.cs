using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using Phonebook.Web.Models.Entities;

namespace Phonebook.Web.Data
{
    /// <summary>
    /// EF6 DbContext for the Phonebook database.
    /// Uses ASP.NET Identity for authentication and stores contacts globally.
    /// </summary>
    public class PhonebookDbContext : IdentityDbContext<ApplicationUser>
    {
        public PhonebookDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
            // The initializer is registered once at application start
            // (see Global.asax). Do not reset it here — that would
            // prevent schema creation and seeding on first run.
        }

        public static PhonebookDbContext Create()
        {
            return new PhonebookDbContext();
        }

        public DbSet<Contact> Contacts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map Identity tables to custom names (matching source schema)
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUserRole>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin>().ToTable("AspNetUserLogins");

            // Configure Contacts table (EF6 fluent API)
            modelBuilder.Entity<Contact>().HasKey(e => e.Id);
            modelBuilder.Entity<Contact>().Property(e => e.Name).HasMaxLength(255).IsRequired();
            modelBuilder.Entity<Contact>().Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Contact>().Property(e => e.Email).HasMaxLength(255).IsOptional();
            modelBuilder.Entity<Contact>().Property(e => e.Address).IsOptional();
        }
    }
}
