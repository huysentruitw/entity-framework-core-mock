using EntityFrameworkMock.Tests.Models;
using NUnit.Framework;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkMock.NSubstitute.Tests
{
    [TestFixture]
    public class DbContextMockTests
    {
        [Test]
        public void DbContextMock_Constructor_PassConnectionString_ShouldPassConnectionStringToMockedClass()
        {
            var connectionString = Guid.NewGuid().ToString("N");
            var dbContextMock = new DbContextMock<TestDbContext>(connectionString);
            Assert.That(dbContextMock.DbContext.Database.Connection.ConnectionString, Is.EqualTo(connectionString));
        }

        [Test]
        public async Task DbContextMock_Constructor_ShouldSetupSaveChanges()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());
            Assert.That(dbContextMock.DbContext.SaveChanges(), Is.EqualTo(55861));
            Assert.That(await dbContextMock.DbContext.SaveChangesAsync(), Is.EqualTo(55861));
            Assert.That(await dbContextMock.DbContext.SaveChangesAsync(CancellationToken.None), Is.EqualTo(55861));
        }

        [Test]
        public void DbContextMock_Reset_ShouldForgetMockedDbSets()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());
            Assert.That(dbContextMock.DbContext.SaveChanges(), Is.EqualTo(55861));
            dbContextMock.Reset();
            Assert.That(dbContextMock.DbContext.SaveChanges(), Is.EqualTo(0));
        }

        [Test]
        public void DbContextMock_Reset_ShouldResetupSaveChanges()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());
            Assert.That(dbContextMock.DbContext.SaveChanges(), Is.EqualTo(55861));
            dbContextMock.Reset();
            Assert.That(dbContextMock.DbContext.SaveChanges(), Is.EqualTo(0));
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());
            Assert.That(dbContextMock.DbContext.SaveChanges(), Is.EqualTo(55861));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_CreateIdenticalDbSetMockTwice_ShouldThrowExceptionSecondTime()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            dbContextMock.CreateDbSetMock(x => x.Users);
            var ex = Assert.Throws<ArgumentException>(() => dbContextMock.CreateDbSetMock(x => x.Users));
            Assert.That(ex.ParamName, Is.EqualTo("dbSetSelector"));
            Assert.That(ex.Message, Does.StartWith("DbSetMock for Users already created"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_ShouldSetupMockForDbSetSelector()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            Assert.That(dbContextMock.DbContext.Users, Is.Null);
            dbContextMock.CreateDbSetMock(x => x.Users);
            Assert.That(dbContextMock.DbContext.Users, Is.Not.Null);
        }

        [Test]
        public async Task DbContextMock_CreateDbSetMock_PassInitialEntities_DbSetShouldContainInitialEntities()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            dbContextMock.CreateDbSetMock(x => x.Users, new[]
            {
                new User { Id = Guid.NewGuid(), FullName = "Eric Cartoon" },
                new User { Id = Guid.NewGuid(), FullName = "Billy Jewel" },
            });

            Assert.That(dbContextMock.DbContext.Users.Count(), Is.EqualTo(2));
            Assert.That(await dbContextMock.DbContext.Users.CountAsync(), Is.EqualTo(2));

            var result = await dbContextMock.DbContext.Users.FirstAsync(x => x.FullName.StartsWith("Eric"));
            Assert.That(result.FullName, Is.EqualTo("Eric Cartoon"));

            result = dbContextMock.DbContext.Users.First(x => x.FullName.Contains("Jewel"));
            Assert.That(result.FullName, Is.EqualTo("Billy Jewel"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_NoKeyFactoryForModelWithoutKeyAttributes_ShouldThrowException()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            var ex = Assert.Throws<InvalidOperationException>(() => dbContextMock.CreateDbSetMock(x => x.NoKeyModels));
            Assert.That(ex.Message, Is.EqualTo("Entity type NoKeyModel does not contain any property marked with KeyAttribute"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_CustomKeyFactoryForModelWithoutKeyAttributes_ShouldNotThrowException()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            Assert.DoesNotThrow(() => dbContextMock.CreateDbSetMock(x => x.NoKeyModels, (x, _) => x.Id));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_AddModelWithSameKeyTwice_ShouldThrowDbUpdatedException()
        {
            var userId = Guid.NewGuid();
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.Users);
            dbSetMock.DbSet.Add(new User { Id = userId, FullName = "SomeName" });
            dbSetMock.DbSet.Add(new User { Id = Guid.NewGuid(), FullName = "SomeName" });
            dbContextMock.DbContext.SaveChanges();
            dbSetMock.DbSet.Add(new User { Id = userId, FullName = "SomeName" });
            Assert.Throws<DbUpdateException>(() => dbContextMock.DbContext.SaveChanges());
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_DeleteUnknownModel_ShouldThrowDbUpdateConcurrencyException()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.Users);
            dbSetMock.DbSet.Remove(new User { Id = Guid.NewGuid() });
            Assert.Throws<DbUpdateConcurrencyException>(() => dbContextMock.DbContext.SaveChanges());
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_AddMultipleModelsWithDatabaseGeneratedIdentityKey_ShouldGenerateSequentialKey()
        {
            var dbContextMock = new DbContextMock<TestDbContext>("abc");
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.GeneratedKeyModels, new[]
            {
                new GeneratedKeyModel {Value = "first"},
                new GeneratedKeyModel {Value = "second"}
            });
            dbSetMock.DbSet.Add(new GeneratedKeyModel { Value = "third" });
            dbContextMock.DbContext.SaveChanges();

            Assert.That(dbSetMock.DbSet.Min(x => x.Id), Is.EqualTo(1));
            Assert.That(dbSetMock.DbSet.Max(x => x.Id), Is.EqualTo(3));
            Assert.That(dbSetMock.DbSet.First(x => x.Id == 1).Value, Is.EqualTo("first"));
            Assert.That(dbSetMock.DbSet.First(x => x.Id == 2).Value, Is.EqualTo("second"));
            Assert.That(dbSetMock.DbSet.First(x => x.Id == 3).Value, Is.EqualTo("third"));
        }

        public class TestDbSetMock : IDbSetMock
        {
            public int SaveChanges() => 55861;
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(string connectionString)
                : base(connectionString)
            {
                ConnectionString = connectionString;
            }

            public string ConnectionString { get; }

            public virtual DbSet<User> Users { get; set; }

            public virtual DbSet<NoKeyModel> NoKeyModels { get; set; }

            public virtual DbSet<GeneratedKeyModel> GeneratedKeyModels { get; set; }
        }
    }
}