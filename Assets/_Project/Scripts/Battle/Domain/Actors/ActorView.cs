namespace MurimRunaway.Battle.Domain
{
    /// <summary>매 틱 View로 전달되는 액터 읽기 전용 사본.</summary>
    public readonly struct ActorView
    {
        public readonly int Id;
        public readonly bool IsPlayer;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly int Mana;
        public readonly int MaxMana;
        public readonly int Momentum;
        public readonly int MaxMomentum;
        public readonly SkillSlotView[] Skills;
        public readonly float Position;
        public readonly ActorState State;

        public ActorView(Actor actor)
        {
            Id = actor.Id;
            IsPlayer = actor is PlayerActor;
            Hp = actor.Hp;
            MaxHp = actor.MaxHp;
            Position = actor.Position;
            State = actor.State;

            if (actor is PlayerActor player)
            {
                Mana = player.Mana;
                MaxMana = player.MaxMana;
                Momentum = player.Momentum;
                MaxMomentum = player.MaxMomentum;
                Skills = ToSlotViews(player.Skills);
            }
            else
            {
                Mana = 0;
                MaxMana = 0;
                Momentum = 0;
                MaxMomentum = 0;
                Skills = System.Array.Empty<SkillSlotView>();
            }
        }

        private static SkillSlotView[] ToSlotViews(SkillSlot[] slots)
        {
            var views = new SkillSlotView[slots.Length];
            for (var index = 0; index < slots.Length; index++)
                views[index] = new SkillSlotView(slots[index]);
            return views;
        }
    }
}
