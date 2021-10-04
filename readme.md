# fluent-bogus
[Bogus](https://github.com/bchavez/Bogus) based fake object generator using fluent syntax



### Usage example:

#### Generate a fake object:
```csharp
var user = new FluentFaker<User>().Build();
```

#### Generate multiple objects:
```csharp
var users = new FluentFaker<User>().BuildMany(3);
```

#### Include navigation properties:
```csharp
var users = new FluentFaker<User>()
    .Include(u => u.Company.BillingAddress)
    .Include(u => u.Company.Departments)
    .BuildMany(3);
```


#### To extend with custom faker rules, just create a new class derived from FluentFaker<> and specify the assembly where it's defined:
```csharp
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

public void Setup()
{
    FluentFaker.Setup(new FluentFakerOptions
    {
        FakeBuilderAssemblies = new List<Assembly>
        {
            Assembly.GetExecutingAssembly(),
            typeof(IFakerAssemblyMarker).GetTypeInfo().Assembly
        }
    });
}


public void CreateFakes()
{
    var users = new UserFaker()
        .WithPassword("Pa$$wOrd!")
        .Include(u => u.Company.BillingAddress)
        .Include(u => u.Company.Departments)
        .BuildMany(3);
}
```





### Used classes in Usage examples:

#### User:
```csharp
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
```

#### Company:
```csharp
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public CompanyAddress LegalAddress { get; set; }
        public CompanyAddress BillingAddress { get; set; }

        public List<Department> Departments { get; set; }
    }
```



#### CompanyAddress
```csharp
   public class CompanyAddress
    {
        public int Id { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string StreetAddress { get; set; }
        public string ZipCode { get; set; }
    }
```


#### Department
```csharp
    public class Department
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
```










