using FluentBogus.Tests.Models;

namespace FluentBogus.Tests.FakeBuilders
{
    public class DepartmentFaker : FluentFaker<Department>
    {
        public DepartmentFaker()
        {
            Faker.RuleFor(u => u.Name, f => f.Person.Company.Name);
        }
    }
}
