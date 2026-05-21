namespace MurimRunaway.Battle.Domain
{
    /// <summary>런타임 액터 인스턴스의 공통 base. Engine만 mutable 권한을 가진다.</summary>
    public abstract class Actor
    {
        public int Id;
        public int Hp;
        public int MaxHp;
        public float Position;
        public ActorState State;
        public string SourceId;
        public float EngageDistance;

        public ActorView ToView() => new(this);
    }
}
