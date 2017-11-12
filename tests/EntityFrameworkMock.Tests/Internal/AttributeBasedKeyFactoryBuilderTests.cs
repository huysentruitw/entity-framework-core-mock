using System;
using System.ComponentModel.DataAnnotations;
using EntityFrameworkMock.Internal;
using EntityFrameworkMock.Tests.Models;
using NUnit.Framework;

namespace EntityFrameworkMock.Tests.Internal
{
    [TestFixture]
    public class AttributeBasedKeyFactoryBuilderTests
    {
        [Test]
        public void AttributeBasedKeyFactoryBuilder_GivenModelWithOneKeyProperty_ShouldReturnCorrectKey()
        {
            var builder = new AttributeBasedKeyFactoryBuilder<KeyAttribute>();
            var factory = builder.BuildKeyFactory<User>();
            var userId = Guid.NewGuid();
            var key = factory(new User {Id = userId, FullName = "Jake Snake"});
            Assert.That(key, Is.EqualTo(new Tuple<Guid>(userId)));
        }

        [Test]
        public void AttributeBasedKeyFactoryBuilder_GivenModelWithTwoKeyProperties_ShouldReturnCorrectKey()
        {
            var builder = new AttributeBasedKeyFactoryBuilder<KeyAttribute>();
            var factory = builder.BuildKeyFactory<TenantUser>();
            var userId = Guid.NewGuid();
            var tenant = Guid.NewGuid().ToString("N");
            var key = factory(new TenantUser { Id = userId, Tenant = tenant, FullName = "Jake Snake" });
            Assert.That(key, Is
                .EqualTo(new Tuple<Guid, string>(userId, tenant))
                .Or
                .EqualTo(new Tuple<string, Guid>(tenant, userId)));
        }
    }
}
