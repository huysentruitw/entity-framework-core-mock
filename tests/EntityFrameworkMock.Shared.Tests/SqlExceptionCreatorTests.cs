using AutoFixture;
using NUnit.Framework;

namespace EntityFrameworkMock.Shared.Tests
{
    [TestFixture]
    public class SqlExceptionCreatorTests
    {
        [Test]
        public void Create_ShouldNotReturnNull()
        {
            var fixture = new Fixture();
            var message = fixture.Create<string>();
            var errorCode = fixture.Create<int>();

            System.Data.SqlClient.SqlException exception = SqlExceptionCreator.Create(message, errorCode);

            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public void Create_ShouldCreateSqlException()
        {
            var fixture = new Fixture();
            var message = fixture.Create<string>();
            var errorCode = fixture.Create<int>();

            System.Data.SqlClient.SqlException exception = SqlExceptionCreator.Create(message, errorCode);

            Assert.That(exception.Message, Is.EqualTo(message));
            Assert.That(exception.Number, Is.EqualTo(errorCode));
        }
    }
}
