using System;
using EntityFrameworkCoreMock.Shared.Tests.Models;
using NUnit.Framework;

namespace EntityFrameworkCoreMock.Shared.Tests.KeyFactoryBuilders
{
    [TestFixture]
    public class ConventionBasedKeyFactoryBuilderTests
    {
        [Test]
        public void ConventionBasedKeyFactoryBuilder_GivenModelWithIdProperty_ShouldReturnCorrectKey()
        {
            // Arrange
            var builder = new ConventionBasedKeyFactoryBuilder();
            var factory = builder.BuildKeyFactory<UserWithIdProperty>();
            var id = Guid.NewGuid();

            // Act
            var key = factory(new UserWithIdProperty { Id = id, FullName = "Jake Snake" }, null);

            // Assert
            Assert.That(key, Is.EqualTo(new Tuple<Guid>(id)));
        }

        [Test]
        public void ConventionBasedKeyFactoryBuilder_GivenModelWithClassPrefixedIdProperty_ShouldReturnCorrectKey()
        {
            // Arrange
            var builder = new ConventionBasedKeyFactoryBuilder();
            var factory = builder.BuildKeyFactory<UserWithKeyByConvention>();
            var id = Guid.NewGuid();

            // Act
            var key = factory(new UserWithKeyByConvention { UserWithKeyByConventionId = id, FullName = "Jake Snake" }, null);

            // Assert
            Assert.That(key, Is.EqualTo(new Tuple<Guid>(id)));
        }

        [Test]
        public void ConventionBasedKeyFactoryBuilder_GivenModelWithMultipleConventionBasedIdProperties_ShouldThrowException()
        {
            // Arrange
            var builder = new ConventionBasedKeyFactoryBuilder();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildKeyFactory<UserWithMultipleKeysByConvention>());
            Assert.That(exception.Message, Is.EqualTo("Entity type UserWithMultipleKeysByConvention contains multiple conventional id properties"));
        }

        [Test]
        public void ConventionBasedKeyFactoryBuilder_GivenModelWithoutIdProperty_ShouldThrowException()
        {
            // Arrange
            var builder = new ConventionBasedKeyFactoryBuilder();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildKeyFactory<UserWithoutKeyByConvention>());
            Assert.That(exception.Message, Is.EqualTo("Entity type UserWithoutKeyByConvention does not contain any property named Id or UserWithoutKeyByConventionId"));
        }
    }
}
