using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkMock.Internal;
using EntityFrameworkMock.Tests.Models;
using NUnit.Framework;

namespace EntityFrameworkMock.Tests
{
    [TestFixture]
    public class DbSetMockTests
    {
        [Test]
        public void DbSetMock_GivenEntityIsAdded_ShouldAddAfterCallingSaveChanges()
        {
            var user = new User {Id = Guid.NewGuid(), FullName = "Fake Drake"};
            var dbSetMock = new DbSetMock<User>(null, x => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(dbSet.Count(), Is.EqualTo(0));
            dbSet.Add(user);
            Assert.That(dbSet.Count(), Is.EqualTo(0));
            ((IDbSetMock)dbSetMock).SaveChanges();
            Assert.That(dbSet.Count(), Is.EqualTo(1));
            Assert.That(dbSet.Any(x => x.Id == user.Id && x.FullName == user.FullName), Is.True);
        }

        [Test]
        public async Task DbSetMock_AsyncProvider()
        {
            var user = new User { Id = Guid.NewGuid(), FullName = "Fake Drake" };
            var dbSetMock = new DbSetMock<User>(null, x => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(await dbSet.CountAsync(), Is.EqualTo(0));
            dbSet.Add(user);
            Assert.That(await dbSet.CountAsync(), Is.EqualTo(0));
            ((IDbSetMock)dbSetMock).SaveChanges();
            Assert.That(await dbSet.CountAsync(), Is.EqualTo(1));
            Assert.That(await dbSet.AnyAsync(x => x.Id == user.Id && x.FullName == user.FullName), Is.True);
        }

        [Test]
        public void DbSetMock_GivenEntityIsRemoved_ShouldRemoveAfterCallingSaveChanges()
        {
            var user = new User {Id = Guid.NewGuid(), FullName = "Fake Drake"};
            var dbSetMock = new DbSetMock<User>(new[]
            {
                user,
                new User {Id = Guid.NewGuid(), FullName = "Jackira Spicy"}
            }, x => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(dbSet.Count(), Is.EqualTo(2));
            dbSet.Remove(new User {Id = user.Id});
            Assert.That(dbSet.Count(), Is.EqualTo(2));
            Assert.That(dbSet.Any(x => x.Id == user.Id && x.FullName == user.FullName), Is.True);
            ((IDbSetMock)dbSetMock).SaveChanges();
            Assert.That(dbSet.Count(), Is.EqualTo(1));
            Assert.That(dbSet.Any(x => x.Id == user.Id && x.FullName == user.FullName), Is.False);
        }
    }
}
