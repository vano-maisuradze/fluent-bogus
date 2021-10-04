using FluentAssertions;
using FluentBogus.Tests.FakeBuilders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;

namespace FluentBogus.Tests
{
    public class Tests
    {

        [SetUp]
        public void Setup()
        {
            FluentFaker.Setup(new FluentFakerOptions
            {
                FakeBuilderAssemblies = new List<Assembly>
                {
                    Assembly.GetExecutingAssembly()
                }
            });
        }

        [Test]
        public void ShouldCreateFakeObjectWithNavigationProperties()
        {
            var count = 3;
            var password = "Pa$$wOrd!";

            var users = new UserFaker()
                .WithPassword(password)
                .Include(u => u.Company.BillingAddress)
                .Include(u => u.Company.Departments)
                .BuildMany(count);

            users.Should().NotBeNull();
            users.Should().HaveCount(count);

            foreach (var user in users)
            {
                user.Id.Should().BeGreaterThan(0);
                user.FirstName.Should().NotBeNullOrEmpty();
                user.Password.Should().Be(password);

                user.Company.Should().NotBeNull();
                user.Company.Id.Should().BeGreaterThan(0);

                user.Company.BillingAddress.Should().NotBeNull();
                user.Company.BillingAddress.Id.Should().BeGreaterThan(0);

                user.Company.LegalAddress.Should().BeNull();
                user.Company.Departments.Should().NotBeEmpty();

                foreach (var department in user.Company.Departments)
                {
                    department.Code.Should().NotBeNullOrEmpty();
                }
            }
        }
    }
}