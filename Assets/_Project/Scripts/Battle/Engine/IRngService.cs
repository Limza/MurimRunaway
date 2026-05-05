using System.Collections.Generic;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>모든 난수의 단일 창구. 시드 고정으로 결정적 시뮬레이션 보장.</summary>
    public interface IRngService
    {
        void Reseed(int seed);
        int NextInt(int minIncl, int maxExcl);
        float NextFloat01();
        T Pick<T>(IReadOnlyList<T> pool);
        T PickWeighted<T>(IReadOnlyList<(T item, float weight)> pool);
    }
}