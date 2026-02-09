using UnityEngine;

namespace HitWaves.Core
{
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "HitWaves/Character Stat Data")]
    public class CharacterStatData : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 5f;

        [Header("Combat")]
        public float maxHealth = 100f;
        public float damage = 10f;
        public float attackSpeed = 1f;

        [Header("Physical")]
        public float strength = 10f;

        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.MoveSpeed => moveSpeed,
                StatType.MaxHealth => maxHealth,
                StatType.Damage => damage,
                StatType.AttackSpeed => attackSpeed,
                StatType.Strength => strength,
                _ => 0f
            };
        }
    }
}
