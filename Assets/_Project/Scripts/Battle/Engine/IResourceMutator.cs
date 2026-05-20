namespace MurimRunaway.Battle.Engine
{
    /// <summary>자원 변경의 단일 진입점. 모든 자원 변경은 이 인터페이스만 통과한다.</summary>
    public interface IResourceMutator
    {
        /// <summary>amount만큼 내공 소비. 부족하면 false를 반환하고 값을 그대로 둔다.</summary>
        bool SpendMana(int amount);

        /// <summary>maxMana를 넘지 않게 채운다.</summary>
        void GainMana(int amount);

        /// <summary>amount만큼 기세 소비. 부족하면 false를 반환하고 값을 그대로 둔다.</summary>
        bool SpendMomentum(int amount);

        /// <summary>maxMomentum을 넘지 않게 채운다.</summary>
        void GainMomentum(int amount);
    }
}