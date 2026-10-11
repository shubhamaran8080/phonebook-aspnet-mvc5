using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phonebook.Web.Models.Entities
{
    /// <summary>
    /// A phonebook entry mapped to the "Contacts" table.
    /// Contacts are global and shared among all authenticated users
    /// (no per-user ownership, matching the source application).
    /// </summary>
    [Table("Contacts")]
    public class Contact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("Name")]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        [Column("PhoneNumber")]
        public string PhoneNumber { get; set; }

        [StringLength(255)]
        [Column("Email")]
        public string Email { get; set; }

        [Column("Address")]
        public string Address { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
