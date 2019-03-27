using System;
using System.ComponentModel.DataAnnotations;
using EntityFrameworkCoreMock.Shared.Tests.Models;
using NUnit.Framework;

namespace EntityFrameworkCoreMock.Shared.Tests.KeyFactoryBuilders
{
    [TestFixture]
    public class AttributeBasedKeyFactoryBuilderTests
    {
        [Test]
        public void AttributeBasedKeyFactoryBuilder_GivenModelWithOneKeyProperty_ShouldReturnCorrectKey()
        {
            var builder = new AttributeBasedKeyFactoryBuilder<KeyAttribute>();
            var factory = builder.BuildKeyFactory<UserWithKeyAttribute>();
            var userId = Guid.NewGuid();
            var key = factory(new UserWithKeyAttribute { UserId = userId, FullName = "Jake Snake" }, null);
            Assert.That(key, Is.EqualTo(new Tuple<Guid>(userId)));
        }

        [Test]
        public void AttributeBasedKeyFactoryBuilder_GivenModelWithTwoKeyProperties_ShouldReturnCorrectKey()
        {
            var builder = new AttributeBasedKeyFactoryBuilder<KeyAttribute>();
            var factory = builder.BuildKeyFactory<UserWithAdditionalKeyAttribute>();
            var userId = Guid.NewGuid();
            var tenant = Guid.NewGuid().ToString("N");
            var key = factory(new UserWithAdditionalKeyAttribute { UserId = userId, Tenant = tenant, FullName = "Jake Snake" }, null);
            Assert.That(key, Is
                .EqualTo(new Tuple<Guid, string>(userId, tenant))
                .Or
                .EqualTo(new Tuple<string, Guid>(tenant, userId)));
        }

        [Test]
        public void AttributeBasedKeyFactoryBuilder_GivenModelWithoutKeyProperties_ShouldThrowException()
        {
            var builder = new AttributeBasedKeyFactoryBuilder<KeyAttribute>();
            var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildKeyFactory<UserWithoutKeyAttribute>());
            Assert.That(exception.Message, Is.EqualTo("Entity type UserWithoutKeyAttribute does not contain any property marked with KeyAttribute"));
        }
    }
}
