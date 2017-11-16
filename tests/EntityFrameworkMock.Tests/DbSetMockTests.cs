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

        [Test]
        public void DbSetMock_GivenEntityPropertyIsChangedAndSaveChangesIsCalled_ShouldFireSavedChangesEventWithCorrectUpdatedInfo()
        {
            var userId = Guid.NewGuid();
            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User {Id = userId, FullName = "Mark Kramer"},
                new User {Id = Guid.NewGuid(), FullName = "Freddy Kipcurry"}
            }, x => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(dbSet.Count(), Is.EqualTo(2));
            var fetchedUser = dbSet.First(x => x.Id == userId);
            fetchedUser.FullName = "Kramer Mark";

            SavedChangesEventArgs<User> eventArgs = null;
            dbSetMock.SavedChanges += (sender, args) => eventArgs = args;

            ((IDbSetMock)dbSetMock).SaveChanges();

            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs.UpdatedEntities, Has.Length.EqualTo(1));
            var updatedEntity = eventArgs.UpdatedEntities[0];
            Assert.That(updatedEntity.UpdatedProperties, Has.Length.EqualTo(1));
            var updatedProperty = updatedEntity.UpdatedProperties[0];
            Assert.That(updatedProperty.Name, Is.EqualTo("FullName"));
            Assert.That(updatedProperty.Original, Is.EqualTo("Mark Kramer"));
            Assert.That(updatedProperty.New, Is.EqualTo("Kramer Mark"));
        }
    }
}
