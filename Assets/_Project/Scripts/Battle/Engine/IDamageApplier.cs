using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    public interface IDamageApplier
    {
        void ApplyDamage(Actor source, Actor target, int amount, DamageKind damageKind, string skillId);
    }
}
