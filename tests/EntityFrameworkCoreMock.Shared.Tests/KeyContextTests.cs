using NUnit.Framework;

namespace EntityFrameworkCoreMock.Shared.Tests
{
    [TestFixture]
    public class KeyContextTests
    {
        [Test]
        public void NextIdentity_FirstCall_ShouldReturnOne()
        {
            var keyContext = new KeyContext();
            Assert.That(keyContext.NextIdentity, Is.EqualTo(1));
        }

        [Test]
        public void NextIdentity_CallTenTimes_ShouldIncrementEachCall()
        {
            var keyContext = new KeyContext();
            for (var i = 1; i <= 10; i++)
            {
                Assert.That(keyContext.NextIdentity, Is.EqualTo(i));
            }
        }
    }
}
