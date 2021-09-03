using FluentBogus.Tests.Models;

namespace FluentBogus.Tests.FakeBuilders
{
    public class DepartmentBuilder : FluentFaker<Department>
    {
        public DepartmentBuilder()
        {
            Faker.RuleFor(u => u.Name, f => f.Person.Company.Name);
        }
    }
}
