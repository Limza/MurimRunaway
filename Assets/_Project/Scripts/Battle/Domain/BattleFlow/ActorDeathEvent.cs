namespace MurimRunaway.Battle.Domain
{
    /// <summary>액터가 사망했을 때 View와 테스트에 알리는 사건.</summary>
    public readonly struct ActorDeathEvent
    {
        public readonly int ActorId;

        public ActorDeathEvent(int actorId)
        {
            ActorId = actorId;
        }
    }
}