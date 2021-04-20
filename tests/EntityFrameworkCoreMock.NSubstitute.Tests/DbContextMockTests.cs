using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCoreMock.Tests.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EntityFrameworkCoreMock.NSubstitute.Tests
{
    [TestFixture]
    public class DbContextMockTests
    {
        [Test]
        public void DbContextMock_Constructor_PassDbContextOptions_ShouldPassDbContextOptionsToMockedClass()
        {
            // Act
            var dbContextMock = new DbContextMock<TestDbContext>(Options);

            // Assert
            Assert.That(dbContextMock.Object.Options, Is.EqualTo(Options));
        }

        [Test]
        public async Task DbContextMock_Constructor_ShouldSetupSaveChanges()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);

            // Act
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());

            // Assert
            Assert.That(dbContextMock.Object.SaveChanges(), Is.EqualTo(55861));
            Assert.That(dbContextMock.Object.SaveChanges(true), Is.EqualTo(55861));
            Assert.That(await dbContextMock.Object.SaveChangesAsync(), Is.EqualTo(55861));
            Assert.That(await dbContextMock.Object.SaveChangesAsync(CancellationToken.None), Is.EqualTo(55861));
        }

        [Test]
        public void DbContextMock_Reset_ShouldForgetMockedDbSets()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());
            Assert.That(dbContextMock.Object.SaveChanges(), Is.EqualTo(55861));

            // Act
            dbContextMock.Reset();

            // Assert
            Assert.That(dbContextMock.Object.SaveChanges(), Is.EqualTo(0));
        }

        [Test]
        public void DbContextMock_Reset_ShouldResetupSaveChanges()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());
            Assert.That(dbContextMock.Object.SaveChanges(), Is.EqualTo(55861));
            dbContextMock.Reset();
            Assert.That(dbContextMock.Object.SaveChanges(), Is.EqualTo(0));

            // Act
            dbContextMock.RegisterDbSetMock(x => x.Users, new TestDbSetMock());

            // Assert
            Assert.That(dbContextMock.Object.SaveChanges(), Is.EqualTo(55861));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_CreateIdenticalDbSetMockTwice_ShouldThrowExceptionSecondTime()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            dbContextMock.CreateDbSetMock(x => x.Users);

            // Act
            void CreateDbMock()
            {
                dbContextMock.CreateDbSetMock(x => x.Users);
            }

            // Assert
            var ex = Assert.Throws<ArgumentException>(CreateDbMock);
            Assert.That(ex.ParamName, Is.EqualTo("dbSetSelector"));
            Assert.That(ex.Message, Does.StartWith("DbSetMock for Users already created"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_ShouldSetupMockForDbSetSelector()
        {
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            Assert.Throws<NotImplementedException>(() => dbContextMock.Object.Users.ToArray());
            dbContextMock.CreateDbSetMock(x => x.Users);
            Assert.That(dbContextMock.Object.Users, Is.Not.Null);
        }

        [Test]
        public async Task DbContextMock_CreateDbSetMock_PassInitialEntities_DbSetShouldContainInitialEntities()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);

            // Act
            dbContextMock.CreateDbSetMock(x => x.Users, new[]
            {
                new User { Id = Guid.NewGuid(), FullName = "Eric Cartoon" },
                new User { Id = Guid.NewGuid(), FullName = "Billy Jewel" },
            });

            // Assert
            Assert.That(dbContextMock.Object.Users.Count(), Is.EqualTo(2));
            Assert.That(await dbContextMock.Object.Users.CountAsync(), Is.EqualTo(2));

            var result = await dbContextMock.Object.Users.FirstAsync(x => x.FullName.StartsWith("Eric"));
            Assert.That(result.FullName, Is.EqualTo("Eric Cartoon"));

            result = dbContextMock.Object.Users.First(x => x.FullName.Contains("Jewel"));
            Assert.That(result.FullName, Is.EqualTo("Billy Jewel"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_NoKeyFactoryForModelWithoutId_ShouldThrowException()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);

            // Act & Assert
            var ex = Assert.Throws<AggregateException>(() => dbContextMock.CreateDbSetMock(x => x.NoKeyModels));
            Assert.That(ex.Message, Does.StartWith("No key factory could be created for entity type NoKeyModel, see inner exceptions"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_PassCustomKeyFactoryForModelWithoutId_ShouldNotThrowException()
        {
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            Assert.DoesNotThrow(() =>
                dbContextMock.CreateDbSetMock(
                    x => x.NoKeyModels, (x, _) => x.ModelId));
        }

        [Ignore("Not yet ported to EntityFrameworkCoreMock")]
        public void DbContextMock_CreateDbSetMock_AddModelWithSameKeyTwice_ShouldThrowDbUpdatedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.Users);
            dbSetMock.Object.Add(new User { Id = userId, FullName = "SomeName" });
            dbSetMock.Object.Add(new User { Id = Guid.NewGuid(), FullName = "SomeName" });
            dbContextMock.Object.SaveChanges();
            dbSetMock.Object.Add(new User { Id = userId, FullName = "SomeName" });

            // Act
            void CallSaveChanges()
            {
                dbContextMock.Object.SaveChanges();
            }

            // Assert
            Assert.Throws<DbUpdateException>(CallSaveChanges);
        }

        [Ignore("Not yet ported to EntityFrameworkCoreMock")]
        public void DbContextMock_CreateDbSetMock_DeleteUnknownModel_ShouldThrowDbUpdateConcurrencyException()
        {
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.Users);
            dbSetMock.Object.Remove(new User { Id = Guid.NewGuid() });
            Assert.Throws<DbUpdateConcurrencyException>(() => dbContextMock.Object.SaveChanges());
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_AddMultipleModelsWithDatabaseGeneratedIdentityKey_ShouldGenerateSequentialKey()
        {
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.GeneratedKeyModels, new[]
            {
                new GeneratedKeyModel {Value = "first"},
                new GeneratedKeyModel {Value = "second"}
            });
            dbSetMock.Object.Add(new GeneratedKeyModel { Value = "third" });
            dbContextMock.Object.SaveChanges();

            Assert.That(dbSetMock.Object.Min(x => x.Id), Is.EqualTo(1));
            Assert.That(dbSetMock.Object.Max(x => x.Id), Is.EqualTo(3));
            Assert.That(dbSetMock.Object.First(x => x.Id == 1).Value, Is.EqualTo("first"));
            Assert.That(dbSetMock.Object.First(x => x.Id == 2).Value, Is.EqualTo("second"));
            Assert.That(dbSetMock.Object.First(x => x.Id == 3).Value, Is.EqualTo("third"));
        }

        [Test]
        public void DbContextMock_CreateDbSetMock_AddMultipleModelsWithGuidAsDatabaseGeneratedIdentityKey_ShouldGenerateRandomGuidAsKey()
        {
            var knownId = Guid.NewGuid();
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.GeneratedGuidKeyModels, new[]
            {
                new GeneratedGuidKeyModel {Id = knownId, Value = "first"},
                new GeneratedGuidKeyModel {Value = "second"}
            });
            dbSetMock.Object.Add(new GeneratedGuidKeyModel { Value = "third" });
            dbContextMock.Object.SaveChanges();

            var modelWithKnownId = dbSetMock.Object.FirstOrDefault(x => x.Id == knownId);
            Assert.That(modelWithKnownId, Is.Not.Null);
            Assert.That(modelWithKnownId.Value, Is.EqualTo("first"));
        }

        [Test]
        public async Task DbContextMock_CreateDbSetMock_AsyncAddMultipleModelsWithLongAsDatabaseGeneratedIdentityKey_ShouldGenerateIncrementalKey()
        {
            // Arrange
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.Issue20Models);

            // Act
            await dbContextMock.Object.Issue20Models.AddAsync(new Issue20Model { Url = "A" });
            await dbContextMock.Object.Issue20Models.AddAsync(new Issue20Model { Url = "B" });
            await dbContextMock.Object.Issue20Models.AddAsync(new Issue20Model { Url = "C" });
            await dbContextMock.Object.SaveChangesAsync();

            // Assert
            Assert.That(dbSetMock.Object.First(x => x.Url == "A").LoggingRepositoryId, Is.EqualTo(1));
            Assert.That(dbSetMock.Object.First(x => x.Url == "B").LoggingRepositoryId, Is.EqualTo(2));
            Assert.That(dbSetMock.Object.First(x => x.Url == "C").LoggingRepositoryId, Is.EqualTo(3));
        }

        [Test]
        public void DbContextMock_MultipleGets_ShouldReturnDataEachTime()
        {
            // Arrange
            var users = new List<User>()
            {
                new User() { Id = Guid.NewGuid(), FullName = "Ian Kilmister" },
                new User() { Id = Guid.NewGuid(), FullName = "Phil Taylor" },
                new User() { Id = Guid.NewGuid(), FullName = "Eddie Clarke" }
            };

            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            dbContextMock.CreateDbSetMock(x => x.Users, users);

            List<string> results = new List<string>();

            // Act
            for (int i = 1; i <= 100; i++)
            {
                var readUsers = dbContextMock.Object.Users;
                foreach (var user in readUsers)
                {
                    results.Add(user.FullName);
                }
            }

            // Assert
            Assert.AreEqual(300, results.Count());
            foreach (var result in results)
            {
                Assert.IsNotEmpty(result);
            }
        }

        [Test]
        public void DbContextMock_GenericSet_ShouldReturnDbSetMock()
        {
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            var dbSetMock = dbContextMock.CreateDbSetMock(x => x.Users);
            var dbSet = dbContextMock.Object.Set<User>();
            Assert.That(dbSet, Is.Not.Null);
            Assert.That(dbSet, Is.EqualTo(dbSetMock.Object));
        }
        
        [Test]
        public void DbContextMock_BeginTransaction_CommitTransaction_ShouldNotFail()
        {
            var dbContextMock = new DbContextMock<TestDbContext>(Options);
            Assert.DoesNotThrow(() =>
            {
                var transaction = dbContextMock.Object.Database.BeginTransaction();
                transaction.Commit();
                transaction.Rollback();
            });
            
            Assert.DoesNotThrowAsync(async () =>
            {
                var transaction = await dbContextMock.Object.Database.BeginTransactionAsync();
                await transaction.CommitAsync();
                await transaction.RollbackAsync();
            });
        }

        public class TestDbSetMock : IDbSetMock
        {
            public int SaveChanges() => 55861;
        }

        public class TestDbQueryMock : IDbQueryMock
        {
        }

        public DbContextOptions Options { get; } = new DbContextOptionsBuilder().Options;

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options)
                : base(options)
            {
                Options = options;
            }

            public DbContextOptions Options { get; }

            public virtual DbSet<User> Users { get; set; }

            public virtual DbSet<NoKeyModel> NoKeyModels { get; set; }

            public virtual DbSet<GeneratedKeyModel> GeneratedKeyModels { get; set; }

            public virtual DbSet<GeneratedGuidKeyModel> GeneratedGuidKeyModels { get; set; }

            public virtual DbSet<Issue20Model> Issue20Models { get; set; }
        }
    }
}
