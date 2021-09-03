using Bogus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentBogus
{
    public class FluentFaker
    {
        protected static Dictionary<Type, Type> FakeBuilders { get; private set; }

        public static void Setup(FluentFakerOptions options)
        {
            if (options == null || options.FakeBuilderAssemblies == null)
            {
                return;
            }

            FakeBuilders = new Dictionary<Type, Type>();

            var fakeBuildersList = new List<Type>();
            options.FakeBuilderAssemblies.ForEach(assembly =>
            {
                var types = assembly.GetTypes()
                    .Where(t => t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(FluentFaker<>));

                fakeBuildersList.AddRange(types);
            });

            fakeBuildersList.ForEach(t =>
            {
                if (!FakeBuilders.ContainsKey(t))
                {
                    var genericTypeArguments = t.BaseType.GetGenericArguments();
                    if (genericTypeArguments != null && genericTypeArguments.Length == 1)
                    {
                        FakeBuilders.Add(genericTypeArguments[0], t);
                    }
                }
            });
        }
    }

    public class FluentFaker<T> : FluentFaker where T : class
    {
        public Faker<T> Faker { get; private set; }

        private readonly Dictionary<string, object> _includedProperties = new Dictionary<string, object>();
        private readonly List<LambdaExpression> _expressions = new List<LambdaExpression>();

        public FluentFaker()
        {
            Faker = new Faker<T>();
            SetDefaultRules();
        }

        private void SetDefaultRules()
        {
            var properties = typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite).ToList();

            foreach (var property in properties)
            {
                SetDefaultRule(property);
            }
        }

        private void SetDefaultRule(PropertyInfo pi)
        {
            if (pi.PropertyType == typeof(int))
            {
                Faker.RuleFor(pi.Name, s => s.Random.Int(1, 1000000000));
            }
            else if (pi.PropertyType == typeof(long))
            {
                Faker.RuleFor(pi.Name, s => s.Random.Long(1, 1000000000));
            }
            else if (pi.PropertyType == typeof(decimal))
            {
                Faker.RuleFor(pi.Name, s => s.Random.Decimal(1, 1000));
            }
            else if (pi.PropertyType == typeof(double))
            {
                Faker.RuleFor(pi.Name, s => s.Random.Double(1, 1000));
            }
            else if (pi.PropertyType.IsEnum)
            {
                Faker.RuleFor(pi.Name, s => 0);
            }
            else if (pi.PropertyType == typeof(string))
            {
                Faker.RuleFor(pi.Name, s => s.Random.Word());
            }
            else if (pi.PropertyType == typeof(Guid))
            {
                Faker.RuleFor(pi.Name, s => s.Random.Guid());
            }
            else if (pi.PropertyType == typeof(DateTime))
            {
                Faker.RuleFor(pi.Name, s => s.Date.Recent());
            }
        }

        public virtual T Build()
        {
            CopyRootFakerRules();
            return BuildInternal();
        }

        public virtual List<T> BuildMany(int count = 1)
        {
            CopyRootFakerRules();
            return BuildManyInternal(count);
        }

        private void CopyRootFakerRules()
        {
            FluentFaker.FakeBuilders.TryGetValue(typeof(T), out var builderType);

            if (builderType == null)
            {
                return;
            }

            var fakeBuilder = Activator.CreateInstance(builderType);
            if (fakeBuilder == null)
            {
                return;
            }

            Faker = ((FluentFaker<T>)fakeBuilder).Faker;
        }

        private T BuildInternal()
        {
            var instance = Faker.Generate();

            if (_includedProperties.Count == 0)
            {
                return instance;
            }

            var instanceType = instance.GetType();
            var properties = instanceType.GetProperties().Where(p => p.CanWrite);

            ReplaceInstanceIdInNavigationProperties(instanceType, instance);

            foreach (var property in properties)
            {
                if (_includedProperties.ContainsKey(property.Name))
                {
                    SetPropertyValue(property, instance, instanceType);
                }
            }
            return instance;
        }

        private List<T> BuildManyInternal(int count = 1)
        {
            var fakes = new List<T>();
            for (int i = 0; i < count; i++)
            {
                fakes.Add(BuildInternal());
                _includedProperties.Clear();
                _expressions.ForEach(IncludeInternal);
            }

            return fakes;
        }

        private void SetPropertyValue(PropertyInfo property, T instance, Type instanceType)
        {
            var value = _includedProperties[property.Name];
            property.SetValue(instance, value);

            SetPropertyIdValue(property, instance, instanceType, value);
        }

        private static void SetPropertyIdValue(PropertyInfo property, T instance, Type instanceType, object value)
        {
            var idInfo = instanceType.GetProperty($"{property.Name}Id");
            if (idInfo != null)
            {
                var valueType = value.GetType();
                var id = valueType.GetProperty("Id");
                if (id != null)
                {
                    idInfo.SetValue(instance, id.GetValue(value));
                }
            }
        }

        private void ReplaceInstanceIdInNavigationProperties(Type instanceType, T instance)
        {
            var idInfo = instanceType.GetProperty("Id");
            if (idInfo == null)
            {
                return;
            }

            foreach (var includedProperty in _includedProperties)
            {
                var propertyType = includedProperty.Value.GetType();

                if (IsEnumerableType(propertyType))
                {
                    ReplaceIdInEnumerable(instanceType, instance, includedProperty, idInfo);
                }
                else
                {
                    ReplaceIdInNavigationProperty(instanceType, instance, idInfo, includedProperty);
                }
            }
        }

        private static void ReplaceIdInNavigationProperty(Type instanceType, T instance, PropertyInfo idInfo,
            KeyValuePair<string, object> includedProperty)
        {
            var propertyType = includedProperty.Value.GetType();

            var navigationIdInfo = propertyType.GetProperty($"{instanceType.Name}Id");
            if (navigationIdInfo != null)
            {
                var id = idInfo.GetValue(instance);
                navigationIdInfo.SetValue(includedProperty.Value, id);
            }
        }

        private static void ReplaceIdInEnumerable(Type instanceType, T instance,
            KeyValuePair<string, object> includedProperty, PropertyInfo idInfo)
        {
            var propertyType = includedProperty.Value.GetType();
            var itemType = propertyType.GetGenericArguments()[0];
            var navigationObjects = includedProperty.Value as IEnumerable;

            var navigationIdInfo = itemType.GetProperty($"{instanceType.Name}Id");
            if (navigationObjects != null && navigationIdInfo != null)
            {
                var id = idInfo.GetValue(instance);
                foreach (var navigationObject in navigationObjects)
                {
                    navigationIdInfo.SetValue(navigationObject, id);
                }
            }
        }

        public TBuilder AsBuilder<TBuilder>() where TBuilder : FluentFaker<T>
        {
            return (TBuilder)this;
        }

        public FluentFaker<T> Include<TProperty>(Expression<Func<T, TProperty>> expression) where TProperty : class
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            _expressions.Add(expression);

            IncludeInternal(expression);

            return this;
        }

        private void IncludeInternal(LambdaExpression lambdaExpression)
        {
            var expressionStack = new Stack<MemberExpression>();
            var memberExpression = lambdaExpression.Body as MemberExpression;
            if (memberExpression != null)
            {
                expressionStack.Push(memberExpression);
            }

            while (memberExpression?.Expression != null)
            {
                memberExpression = memberExpression.Expression as MemberExpression;
                if (memberExpression != null)
                {
                    expressionStack.Push(memberExpression);
                }
            }

            var rootExpression = expressionStack.Pop();
            object parentInstance;
            if (!_includedProperties.ContainsKey(rootExpression.Member.Name))
            {
                parentInstance = BuildInstance(rootExpression.Type);
                _includedProperties[rootExpression.Member.Name] = parentInstance;
            }
            else
            {
                parentInstance = _includedProperties[rootExpression.Member.Name];
            }

            while (expressionStack.TryPop(out var expression))
            {
                var childInstance = BuildInstance(expression.Type);
                SetPropertyIfNull(parentInstance, expression.Member.Name, childInstance);
                parentInstance = childInstance;
            }
        }

        private void SetPropertyIfNull(object instance, string memberName, object value)
        {
            var type = instance.GetType();
            var prop = type.GetProperty(memberName);

            if (prop == null)
            {
                throw new ArgumentException($"Property not found", nameof(memberName));
            }

            var propValue = prop.GetValue(instance);
            if (propValue != null)
            {
                return;
            }

            prop.SetValue(instance, value);

            var propertyId = type.GetProperty($"{memberName}Id");
            if (propertyId != null)
            {
                var valueType = value.GetType();
                var id = valueType.GetProperty("Id");
                if (id != null)
                {
                    propertyId.SetValue(instance, id.GetValue(value));
                }
            }
        }

        private object BuildInstance(Type propertyType)
        {
            if (IsEnumerableType(propertyType))
            {
                return BuildEnumerableInstance(propertyType);
            }

            FluentFaker.FakeBuilders.TryGetValue(propertyType, out var builderType);

            if (builderType == null)
            {
                var genericType = typeof(FluentFaker<>);
                builderType = genericType.MakeGenericType(propertyType);
            }

            var fakeBuilder = Activator.CreateInstance(builderType);
            if (fakeBuilder == null)
            {
                throw new InvalidCastException();
            }

            return ((dynamic)fakeBuilder).BuildInternal();
        }

        private object BuildEnumerableInstance(Type enumerableType)
        {
            var propertyType = enumerableType.GetGenericArguments()[0];
            FluentFaker.FakeBuilders.TryGetValue(propertyType, out var builderType);
            if (builderType == null)
            {
                var genericType = typeof(FluentFaker<>);
                builderType = genericType.MakeGenericType(propertyType);
            }

            var fakeBuilder = Activator.CreateInstance(builderType);
            if (fakeBuilder == null)
            {
                throw new InvalidCastException();
            }

            return ((dynamic)fakeBuilder).BuildManyInternal();
        }

        private bool IsEnumerableType(Type propertyType)
        {
            return propertyType.GetInterfaces()
                .Any(x => x.IsGenericType &&
                          x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
    }
}
