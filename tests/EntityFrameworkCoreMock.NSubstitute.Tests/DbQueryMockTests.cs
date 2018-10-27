using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkCoreMock.Tests.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EntityFrameworkCoreMock.NSubstitute.Tests
{
    [TestFixture]
    public class DbQueryMockTests
    {
        [Test]
        public async Task DbQueryMock_AsyncProvider()
        {
            var model = new QueryModel { AuthorName = "Author 1", ArticleCount = 5 };
            var dbQueryMock = new DbQueryMock<QueryModel>(new [] { model });
            var dbQuery = dbQueryMock.Object;

            Assert.That(await dbQuery.CountAsync(), Is.EqualTo(1));
            Assert.That(await dbQuery.AnyAsync(x => x.AuthorName == "Author 1" && x.ArticleCount == 5), Is.True);
        }

        [Test]
        public void DbQueryMock_Empty_AsEnumerable_ShouldReturnEmptyEnumerable()
        {
            var dbQueryMock = new DbQueryMock<QueryModel>(new List<QueryModel>());
            var models = dbQueryMock.Object.AsEnumerable();
            Assert.That(models, Is.Not.Null);
            Assert.That(models, Is.Empty);
        }

        [Test]
        public void DbQueryMock_AsEnumerable_ShouldReturnEnumerableCollection()
        {
            var dbQueryMock = new DbQueryMock<QueryModel>(new[]
            {
                new QueryModel { AuthorName = "Author 1", ArticleCount = 5 },
                new QueryModel { AuthorName = "Author 2", ArticleCount = 6 }
            });
            var models = dbQueryMock.Object.AsEnumerable();
            Assert.That(models, Is.Not.Null);
            Assert.That(models.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task DbQueryMock_AsyncProvider_ShouldReturnRequestedModel()
        {
            var dbQueryMock = new DbQueryMock<QueryModel>(new[]
            {
                new QueryModel { AuthorName = "Author 1", ArticleCount = 5 },
                new QueryModel { AuthorName = "Author 2", ArticleCount = 6 }
            });

            var model = await dbQueryMock.Object.Where(x => x.AuthorName == "Author 2").FirstOrDefaultAsync();
            Assert.That(model, Is.Not.Null);
            Assert.That(model.ArticleCount, Is.EqualTo(6));
        }
    }
}
