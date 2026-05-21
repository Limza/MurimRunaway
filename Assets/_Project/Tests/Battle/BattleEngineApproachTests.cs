using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class BattleEngineApproachTests
    {
        [Test]
        public void 플레이어가_적_사거리에_도달하면_Idle_Engage_전이된다()
        {
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                enemies: new[] { BattleTestFactory.CreateEnemy("test", 100f) });

            BattleSnapshot last = default;
            engine.SnapshotPublished += s => last = s;

            engine.Start();

            // | 단계                  | 값    | 출처                                       |
            // | --------------------- | ----- | ------------------------------------------ |
            // | Player 시작 위치       | 0     | BattleEngine.Setup 기본값                  |
            // | Enemy 위치            | 100   | EnemyData.SpawnPosition                    |
            // | Player 교전 시작 거리   | 20    | PlayerStartData.EngageDistance             |
            // | Player가 멈추는 위치   | 80    | 거리(20) = 100 − 80                        |
            // | 이동 거리             | 80    | 0 → 80                                     |
            // | Player 이동 속도       | 5     | PlayerStartData.MoveSpeed (units/sec)      |
            // | 도달 시간             | 16초  | 80 ÷ 5                                     |
            // | 틱 간격               | 0.05초 | MockTickService 기본 dt                    |
            // | 총 틱 수              | 320   | 16 ÷ 0.05                                  |
            tick.PumpTicks(320);

            Assert.AreEqual(BattlePhase.Engage, last.Phase);
            Assert.AreEqual(ActorState.Idle, last.Actors[0].State);    // Player
            Assert.AreEqual(80f, last.Actors[0].Position, 1e-4f);      // Player position
            Assert.AreEqual(100f, last.Actors[1].Position, 1e-4f);     // Enemy 고정
        }
    }
}
