using NUnit.Framework;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class RngServiceTests
    {
        [Test]
        public void 같은_시드로_초기화하면_같은_난수_시퀀스가_나온다()
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