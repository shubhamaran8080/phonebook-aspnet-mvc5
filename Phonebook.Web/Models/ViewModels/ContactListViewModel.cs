using System.Collections.Generic;

namespace Phonebook.Web.Models.ViewModels
{
    public class ContactListViewModel
    {
        public List<ContactViewModel> Contacts { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public int RangeStart { get; set; }
        public int RangeEnd { get; set; }
    }

    public class ContactViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
