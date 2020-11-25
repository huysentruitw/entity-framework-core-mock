using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EntityFrameworkCoreMock.Tests.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EntityFrameworkCoreMock.NSubstitute.Tests
{
    [TestFixture]
    public class AutoMapperRelatedTests
    {
        public class UserModel
        {
            public string FullName { get; set; }

            public string Name { get; set; }
        }

        [Test]
        public async Task DbSetMock_AutoMapperProjectTo()
        {
            var mapperConfiguration = new MapperConfiguration(config =>
            {
                config
                    .CreateMap<User, UserModel>()
                    .ForMember(d => d.Name, opt => opt.MapFrom(s => s.FullName));
            });

            var dbSetMock = new DbSetMock<User>(new[]
            {
                new User { Id = Guid.NewGuid(), FullName = "Fake Drake" },
                new User { Id = Guid.NewGuid(), FullName = "Jackira Spicy" }
            }, (x, _) => x.Id);
            var dbSet = dbSetMock.Object;

            Assert.That(await dbSet.CountAsync(), Is.EqualTo(2));

            var models = await dbSet
                .Where(u => u.FullName != null)
                .ProjectTo<UserModel>(mapperConfiguration)
                .ToListAsync();

            Assert.That(models.Count, Is.EqualTo(2));
        }
    }
}
