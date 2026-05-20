using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>무공 시전 실행의 단일 진입점. 비용·쿨·기세·이벤트를 atomic하게 처리.</summary>
    public interface ISkillExecutor
    {
        /// <summary>slotIndex 무공을 시전. 내공 부족 시 false + 아무 변경 없음.</summary>
        bool TryCast(Actor caster, int slotIndex, Actor target);
    }
}