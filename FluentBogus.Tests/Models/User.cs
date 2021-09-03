using System;

namespace FluentBogus.Tests.Models
{
    public class User
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Location { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsActive { get; set; }

        public Company Company { get; set; }
    }
}
