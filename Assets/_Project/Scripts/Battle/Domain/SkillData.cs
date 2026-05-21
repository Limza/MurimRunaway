using UnityEngine;

namespace MurimRunaway.Battle.Domain
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "MurimRunaway/Battle/SkillData")]
    public sealed class SkillData : ScriptableObject
    {
        [Header("무공 정의 — 한 종류의 무공 SO")]

        [Tooltip("snake_case, 같은 종류 안에서 unique")]
        public string Id;

        [Tooltip("표시 이름 문구 키(\"skill.<id>.name\").")]
        public string NameKey;

        [Tooltip("설명 문구 키(\"skill.<id>.desc\").")]
        public string DescKey;

        [Tooltip("무공 종류")]
        public SkillKind Kind = SkillKind.None;

        [Tooltip("시전 비용 (내공)")]
        public int ManaCost;

        [Tooltip("쿨타임 (초)")]
        public float CooldownSec;

        [Tooltip("타겟 범위")]
        public SkillRange PreferredRange = SkillRange.None;

        [Tooltip("시전 성공 시 획득하는 기세")]
        public int MomentumGainOnCast;
    }
}
