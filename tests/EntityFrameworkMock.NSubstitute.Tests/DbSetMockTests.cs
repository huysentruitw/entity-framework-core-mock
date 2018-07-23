using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFrameworkMock.NSubstitute.Tests
{
    [TestFixture]
    public class DbSetMockTests
    {

        [Test]
        public void DbSetMock_AsNoTracking_ShouldBeMocked()
        {
            var dbSetMock = new DbSetMock<Order>(null, (x, _) => x.Id);
            var dbSet = dbSetMock.Object;
            Assert.That(dbSet.AsNoTracking(), Is.EqualTo(dbSet));
        }

        [Test]
        public void DbSetMock_Include_ShouldBeMocked()
        {
            var dbSetMock = new DbSetMock<Order>(null, (x, _) => x.Id);
            var dbSet = dbSetMock.Object;
            Assert.That(dbSet.Include(x => x.User), Is.EqualTo(dbSet));
        }

        [Test]
        public void DbSetMock_GivenEntityIsAdded_ShouldAddAfterCallingSaveChanges()
        {
            var user = new User { Id = Guid.NewGuid(), FullName = "Fake Drake" };
            var dbSetMock = new DbSetMock<User>(null, (x, _) => x.Id);
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
            var dbSetMock = new DbSetMock<User>(null, (x, _) => x.Id);
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
            var user = new User { Id = Guid.NewGuid(), FullName = "Fake Drake" };
            var dbSetMock = new DbSetMock<User>(new[]
            {
                user,
                new User {Id = Guid.NewGuid(), FullName = "Jackira Spicy"}
            }, (x, _) => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(dbSet.Count(), Is.EqualTo(2));
            dbSet.Remove(new User { Id = user.Id });
            Assert.That(dbSet.Count(), Is.EqualTo(2));
            Assert.That(dbSet.Any(x => x.Id == user.Id && x.FullName == user.FullName), Is.True);
            ((IDbSetMock)dbSetMock).SaveChanges();
            Assert.That(dbSet.Count(), Is.EqualTo(1));
            Assert.That(dbSet.Any(x => x.Id == user.Id && x.FullName == user.FullName), Is.False);
        }

        [Test]
        public void DbSetMock_SaveChanges_GivenEntityPropertyIsChanged_ShouldFireSavedChangesEventWithCorrectUpdatedInfo()
        {
            var userId = Guid.NewGuid();
            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User {Id = userId, FullName = "Mark Kramer"},
                new User {Id = Guid.NewGuid(), FullName = "Freddy Kipcurry"}
            }, (x, _) => x.Id);
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

        [Test]
        public void DbSetMock_SaveChanges_GivenEntityPropertyMarkedAsNotMapped_ShouldNotMarkNotMappedPropertyAsModified()
        {
            var dbSetMock = new DbSetMock<NestedModel>(new[]
            {
                new NestedModel {Id = Guid.NewGuid(), NesteDocument = new NestedModel.Document()},
                new NestedModel {Id = Guid.NewGuid(), NesteDocument = new NestedModel.Document()},
                new NestedModel {Id = Guid.NewGuid(), NesteDocument = new NestedModel.Document()}
            }, (x, _) => x.Id);

            SavedChangesEventArgs<NestedModel> eventArgs = null;
            dbSetMock.SavedChanges += (sender, args) => eventArgs = args;

            dbSetMock.Object.First().Value = "abc";

            ((IDbSetMock)dbSetMock).SaveChanges();

            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs.UpdatedEntities, Has.Length.EqualTo(1));
            Assert.That(eventArgs.UpdatedEntities.First().UpdatedProperties, Has.Length.EqualTo(1));
            var updatedProperty = eventArgs.UpdatedEntities.First().UpdatedProperties.First();
            Assert.That(updatedProperty.Name, Is.EqualTo("Value"));
            Assert.That(updatedProperty.Original, Is.EqualTo(null));
            Assert.That(updatedProperty.New, Is.EqualTo("abc"));
        }

        public class NestedModel
        {
            public Guid Id { get; set; }

            public string Value { get; set; }

            [NotMapped]
            public Document NesteDocument
            {
                get { return new Document { Name = Guid.NewGuid().ToString("N") }; }
                // ReSharper disable once ValueParameterNotUsed
                set { }
            }

            public class Document
            {
                public string Name { get; set; }
            }
        }

        [Test]
        public void DbSetMock_SaveChanges_GivenAbstractEntityModel_ShouldNotThrowException()
        {
            var dbSetMock = new DbSetMock<AbstractModel>(new[]
            {
                new ConcreteModel {Id = Guid.NewGuid()}
            }, (x, _) => x.Id);

            ((IDbSetMock)dbSetMock).SaveChanges();
        }

        public abstract class AbstractModel
        {
            public Guid Id { get; set; }
        }

        public class ConcreteModel : AbstractModel
        {
            public string Name { get; set; } = "SomeName";
        }
    }
}