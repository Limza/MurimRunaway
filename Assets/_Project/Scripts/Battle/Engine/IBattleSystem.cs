namespace MurimRunaway.Battle.Engine
{
    /// <summary>매 Tick 한 단계를 진행시키는 시뮬레이션 시스템. 등록 순서 = 실행 순서.</summary>
    public interface IBattleSystem
    {
        void Tick(BattleContext context, float deltaTime);
    }
}