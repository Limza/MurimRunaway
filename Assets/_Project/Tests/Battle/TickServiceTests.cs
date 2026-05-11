using NUnit.Framework;

namespace MurimRunaway.Battle.Tests
{
    public class TickServiceTests
    {
        [Test]
        public void PumpTicks_AccumulatesDtCorrectly()
        {
            var tick = new MockTickService();
            var total = 0f;
            tick.OnTick += dt => total += dt;

            tick.PumpTicks(100);

            Assert.AreEqual(100 * 0.05f, total, 1e-5f);
        }
    }
}