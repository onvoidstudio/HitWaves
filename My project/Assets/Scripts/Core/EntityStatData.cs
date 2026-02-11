using UnityEngine;

namespace HitWaves.Core
{
    [CreateAssetMenu(fileName = "NewEntityStats", menuName = "HitWaves/Entity Stat Data")]
    public class EntityStatData : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 5f;

        [Header("Combat")]
        public float maxHealth = 100f;
        public float healthRegenRate = 0f;
        public float healthRegenDelay = 3f;
        public float healthRegenInterval = 1f;
        public float damage = 10f;
        public float attackSpeed = 1f;
        public float maxHitCount = 1f;

        [Header("Resource")]
        public float maxStamina = 100f;
        public float maxMental = 100f;
        public float staminaRegenRate = 10f;
        public float staminaRegenDelay = 1f;
        public float staminaRegenInterval = 0.5f;

        [Header("Physical")]
        public float strength = 10f;

        [Header("Mobility")]
        public float jumpRange = 0f;
        public float flightDuration = 0f;

        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.MoveSpeed => moveSpeed,
                StatType.MaxHealth => maxHealth,
                StatType.MaxStamina => maxStamina,
                StatType.MaxMental => maxMental,
                StatType.Damage => damage,
                StatType.AttackSpeed => attackSpeed,
                StatType.Strength => strength,
                StatType.JumpRange => jumpRange,
                StatType.FlightDuration => flightDuration,
                StatType.StaminaRegenRate => staminaRegenRate,
                StatType.StaminaRegenDelay => staminaRegenDelay,
                StatType.StaminaRegenInterval => staminaRegenInterval,
                StatType.HealthRegenRate => healthRegenRate,
                StatType.HealthRegenDelay => healthRegenDelay,
                StatType.HealthRegenInterval => healthRegenInterval,
                StatType.MaxHitCount => maxHitCount,
                _ => 0f
            };
        }
    }
}
