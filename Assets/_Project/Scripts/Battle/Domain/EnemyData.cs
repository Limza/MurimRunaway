using UnityEngine;

namespace MurimRunaway.Battle.Domain
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "MurimRunaway/Battle/EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("적 정의 — 전장에 고정 배치되는 적 한 종류의 ScriptableObject")]

        [Tooltip("snake_case, 카테고리 내 unique")]
        public string Id;

        [Tooltip("L10n 키 (\"enemy.<id>\"). Unity Localization String Table 조회.")]
        public string NameKey;

        [Tooltip("최대 체력")]
        public int MaxHp = 30;

        [Tooltip("적은 이 위치에 고정 배치됨")]
        public float SpawnPosition = 100f;
    }
}