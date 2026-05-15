namespace MurimRunaway.Battle.Engine
{
    /// <summary>자원 변경의 단일 진입점. 모든 자원 변경은 이 인터페이스만 통과한다.</summary>
    public interface IResourceMutator
    {
        bool SpendMana(int amount);

        void GainMana(int amount);
    }
}