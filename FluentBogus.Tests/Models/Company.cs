using System.Collections.Generic;

namespace FluentBogus.Tests.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public CompanyAddress LegalAddress { get; set; }
        public CompanyAddress BillingAddress { get; set; }

        public List<Department> Departments { get; set; }
    }
}
