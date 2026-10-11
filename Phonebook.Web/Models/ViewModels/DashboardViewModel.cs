using System.Collections.Generic;

namespace Phonebook.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public long TotalContacts { get; set; }
        public long RecentContacts { get; set; }
        public long WithEmail { get; set; }
        public long WithPhone { get; set; }
        public List<ContactViewModel> RecentList { get; set; }
        public string Username { get; set; }
    }
}
