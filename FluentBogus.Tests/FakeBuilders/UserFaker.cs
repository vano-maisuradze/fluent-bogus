using FluentBogus.Tests.Models;

namespace FluentBogus.Tests.FakeBuilders
{
    public class UserFaker : FluentFaker<User>
    {
        public UserFaker()
        {
            Faker.RuleFor(u => u.FirstName, f => f.Person.FirstName);
            Faker.RuleFor(u => u.LastName, f => f.Person.LastName);
            Faker.RuleFor(u => u.Email, f => f.Person.Email);
            Faker.RuleFor(u => u.Location, f => f.Person.Address.Street);
        }

        public UserFaker WithPassword(string password)
        {
            Faker.RuleFor(u => u.Password, password);
            return this;
        }
    }
}
