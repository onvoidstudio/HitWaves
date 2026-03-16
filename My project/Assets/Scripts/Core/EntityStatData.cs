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

        [Header("Stamina")]
        [Tooltip("최대 스태미나")]
        [Min(0f)]
        public float maxStamina = 100f;

        [Tooltip("스태미나 회복량 (틱당)")]
        [Min(0f)]
        public float staminaRegenRate = 10f;

        [Tooltip("마지막 소모 후 회복 시작 대기시간 (초)")]
        [Min(0f)]
        public float staminaRegenDelay = 1f;

        [Tooltip("스태미나 회복 틱 간격 (초)")]
        [Min(0.01f)]
        public float staminaRegenInterval = 0.2f;

        [Header("Attack")]
        [Tooltip("공격 속도 (초당 공격 횟수)")]
        [Min(0.1f)]
        public float attackSpeed = 2f;

        [Tooltip("넉백 힘 (Impulse)")]
        [Min(0f)]
        public float knockbackForce = 5f;

        [Tooltip("넉백 저항성")]
        [Min(0f)]
        public float knockbackResistance = 0f;

        [Header("Defense")]
        [Tooltip("본체 견고함 (Toughness + Defense 합산이 데미지 이상이면 무효화)")]
        [Min(0f)]
        public float toughness = 0f;

        [Tooltip("장비 방어력 (Toughness + Defense 합산이 데미지 이상이면 무효화)")]
        [Min(0f)]
        public float defense = 0f;

        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.MoveSpeed => moveSpeed,
                StatType.MaxHealth => maxHealth,
                StatType.Damage => damage,
                StatType.MaxStamina => maxStamina,
                StatType.StaminaRegenRate => staminaRegenRate,
                StatType.StaminaRegenDelay => staminaRegenDelay,
                StatType.StaminaRegenInterval => staminaRegenInterval,
                StatType.AttackSpeed => attackSpeed,
                StatType.KnockbackForce => knockbackForce,
                StatType.KnockbackResistance => knockbackResistance,
                StatType.Toughness => toughness,
                StatType.Defense => defense,
                _ => 0f
            };
        }
    }
}
