namespace MurimRunaway.Battle.Engine
{
    /// <summary>모든 난수의 단일 창구. 시드 고정으로 결정적 시뮬레이션 보장.</summary>
    public interface IRngService
    {
        void Reseed(int seed);
        float NextFloat01();
    }
}
