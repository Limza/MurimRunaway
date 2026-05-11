using NUnit.Framework;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class RngServiceTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new RngService();
            var b = new RngService();
            a.Reseed(12345);
            b.Reseed(12345);

            for (var i = 0; i < 100; i++)
                Assert.AreEqual(a.NextFloat01(), b.NextFloat01(), 0f);
        }
    }
}