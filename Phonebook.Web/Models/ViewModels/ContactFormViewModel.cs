using System.ComponentModel.DataAnnotations;

namespace Phonebook.Web.Models.ViewModels
{
    public class ContactFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in E.164 format, e.g. +1234567890")]
        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Must be a valid email address")]
        [StringLength(255)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(1000)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Created")]
        public System.DateTime CreatedAt { get; set; }
    }
}
