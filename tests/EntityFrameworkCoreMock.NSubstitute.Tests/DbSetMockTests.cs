using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCoreMock.Tests.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EntityFrameworkCoreMock.NSubstitute.Tests
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
        public void DbSetMock_GivenEntityRangeIsAdded_ShouldAddAfterCallingSaveChanges()
        {
            // Arrange
            var users = new List<User>()
            {
                new User() { Id = Guid.NewGuid(), FullName = "Ian Kilmister" },
                new User() { Id = Guid.NewGuid(), FullName = "Phil Taylor" },
                new User() { Id = Guid.NewGuid(), FullName = "Eddie Clarke" }
            };
            var dbSetMock = new DbSetMock<User>(null, (x, _) => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(dbSet.Count(), Is.EqualTo(0));

            // Act
            dbSet.AddRange(users);
            ((IDbSetMock)dbSetMock).SaveChanges();

            // Assert            
            var firstUser = users.First();
            Assert.That(dbSet.Count(), Is.EqualTo(3));
            Assert.That(dbSet.Any(x => x.Id == firstUser.Id
                && x.FullName == firstUser.FullName), Is.True);
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
        public void DbSetMock_GivenRangeOfEntitiesIsRemoved_ShouldRemoveAfterCallingSaveChanges()
        {
            var users = new[]
            {
                new User {Id = Guid.NewGuid(), FullName = "User 1"},
                new User {Id = Guid.NewGuid(), FullName = "User 2"},
                new User {Id = Guid.NewGuid(), FullName = "User 3"}
            };
            var dbSetMock = new DbSetMock<User>(users, (x, _) => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(dbSet.Count(), Is.EqualTo(3));
            dbSet.RemoveRange(users.Skip(1));
            Assert.That(dbSet.Count(), Is.EqualTo(3));
            ((IDbSetMock)dbSetMock).SaveChanges();
            Assert.That(dbSet.Count(), Is.EqualTo(1));
            Assert.That(dbSet.Any(x => x.FullName == "User 1"), Is.True);
            Assert.That(dbSet.Any(x => x.FullName == "User 2"), Is.False);
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
                new NestedModel {Id = Guid.NewGuid(), NestedDocument = new NestedModel.Document()},
                new NestedModel {Id = Guid.NewGuid(), NestedDocument = new NestedModel.Document()},
                new NestedModel {Id = Guid.NewGuid(), NestedDocument = new NestedModel.Document()}
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

        [Test]
        public void DbSetMock_Empty_AsEnumerable_ShouldReturnEmptyEnumerable()
        {
            var dbSetMock = new DbSetMock<NestedModel>(new List<NestedModel>(), (x, _) => x.Id);
            var nestedModels = dbSetMock.Object.AsEnumerable();
            Assert.That(nestedModels, Is.Not.Null);
            Assert.That(nestedModels, Is.Empty);
        }

        [Test]
        public void DbSetMock_AsEnumerable_ShouldReturnEnumerableCollection()
        {
            var dbSetMock = new DbSetMock<NestedModel>(new[]
            {
                new NestedModel {Id = Guid.NewGuid(), NestedDocument = new NestedModel.Document()},
                new NestedModel {Id = Guid.NewGuid(), NestedDocument = new NestedModel.Document()}
            }, (x, _) => x.Id);
            var nestedModels = dbSetMock.Object.AsEnumerable();
            Assert.That(nestedModels, Is.Not.Null);
            Assert.That(nestedModels.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task DbSetMock_AsyncProvider_ShouldReturnRequestedModel()
        {
            var userId = Guid.NewGuid();
            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User {Id = userId, FullName = "Mark Kramer"},
                new User {Id = Guid.NewGuid(), FullName = "Freddy Kipcurry"}
            }, (x, _) => x.Id);

            var model = await dbSetMock.Object.Where(x => x.Id == userId).FirstOrDefaultAsync();
            Assert.That(model, Is.Not.Null);
            Assert.That(model.FullName, Is.EqualTo("Mark Kramer"));
        }

        [Test]
        public async Task DbSetMock_AsyncProvider_OrderBy_ShouldReturnRequestedModel()
        {
            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User {Id = Guid.NewGuid(), FullName = "Mark Kramer"},
                new User {Id = Guid.NewGuid(), FullName = "Freddy Kipcurry"}
            }, (x, _) => x.Id);

            var result = await dbSetMock.Object.Where(x => x.Id != Guid.Empty).OrderBy(x => x.FullName).ToListAsync();
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.First().FullName, Is.EqualTo("Freddy Kipcurry"));
        }

        [Test]
        public void DbSetMock_Find_ShouldReturnRequestedModel()
        {
            Guid user1 = Guid.NewGuid(), user2 = Guid.NewGuid();
            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User {Id = user1, FullName = "Mark Kramer"},
                new User {Id = user2, FullName = "Freddy Kipcurry"}
            }, (x, _) => new Tuple<Guid>(x.Id));

            var result = dbSetMock.Object.Find(user2);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.FullName, Is.EqualTo("Freddy Kipcurry"));
        }

        [Test]
        public void DbSetMock_Find_UnknownId_ShouldReturnNull()
        {
            var unknownUser = Guid.NewGuid();
            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User {Id = Guid.NewGuid(), FullName = "Mark Kramer"},
                new User {Id = Guid.NewGuid(), FullName = "Freddy Kipcurry"}
            }, (x, _) => new Tuple<Guid>(x.Id));

            var result = dbSetMock.Object.Find(unknownUser);
            Assert.That(result, Is.Null);
        }

        public class NestedModel
        {
            public Guid Id { get; set; }

            public string Value { get; set; }

            [NotMapped]
            public Document NestedDocument
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