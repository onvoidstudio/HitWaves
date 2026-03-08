using UnityEngine;

namespace HitWaves.Core
{
    [CreateAssetMenu(fileName = "NewEntityStats", menuName = "HitWaves/Entity Stat Data")]
    public class EntityStatData : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("초당 이동 속도 (units/s)")]
        [Min(0f)]
        public float moveSpeed = 5f;

        [Header("Combat")]
        [Tooltip("최대 체력")]
        [Min(1f)]
        public float maxHealth = 100f;

        [Tooltip("기본 공격력")]
        [Min(0f)]
        public float damage = 10f;

        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.MoveSpeed => moveSpeed,
                StatType.MaxHealth => maxHealth,
                StatType.Damage => damage,
                _ => 0f
            };
        }
    }
}
