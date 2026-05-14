using NUnit.Framework;

namespace MurimRunaway.Battle.Tests
{
    public class TickServiceTests
    {
        [Test]
        public void PumpTicks_호출_횟수만큼_deltaTime이_누적된다()
        {
            var tick = new MockTickService();
            var total = 0f;
            tick.Ticked += dt => total += dt;

            tick.PumpTicks(100);

            Assert.AreEqual(100 * 0.05f, total, 1e-5f);
        }
    }
}