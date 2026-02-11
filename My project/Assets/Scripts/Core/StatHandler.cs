using System;
using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core
{
    public class StatHandler : MonoBehaviour
    {
        private const string LOG_TAG = "StatHandler";

        [SerializeField] private EntityStatData baseStats;

        private Dictionary<StatType, float> _statModifiers;

        public event Action<StatType, float> OnStatChanged;

        private void Awake()
        {
            InitializeModifiers();

            if (baseStats == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: baseStats가 할당되지 않음. 기본값 0 사용.", this);
            }
        }

        private void InitializeModifiers()
        {
            if (_statModifiers != null) return;

            _statModifiers = new Dictionary<StatType, float>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                _statModifiers[stat] = 0f;
            }
        }

        public float GetStat(StatType statType)
        {
            InitializeModifiers();

            if (baseStats == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: baseStats가 null. {statType}에 대해 modifier만 반환.", this);
            }

            float baseValue = baseStats != null ? baseStats.GetStat(statType) : 0f;
            float modifier = _statModifiers.TryGetValue(statType, out float mod) ? mod : 0f;
            return baseValue + modifier;
        }

        public void AddModifier(StatType statType, float amount)
        {
            if (_statModifiers == null)
            {
                DebugLogger.LogError(LOG_TAG, $"{gameObject.name}: _statModifiers가 초기화되지 않음. Awake 호출 전 접근 시도.", this);
                return;
            }

            if (!_statModifiers.ContainsKey(statType))
            {
                _statModifiers[statType] = 0f;
            }

            _statModifiers[statType] += amount;

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: {statType} modifier +{amount} → 현재 총 {GetStat(statType)}", this);

            OnStatChanged?.Invoke(statType, GetStat(statType));
        }

        public void RemoveModifier(StatType statType, float amount)
        {
            AddModifier(statType, -amount);
        }

        public void SetBaseStats(EntityStatData newBaseStats)
        {
            if (newBaseStats == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: null인 EntityStatData 설정 시도.", this);
            }

            baseStats = newBaseStats;

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: baseStats 변경됨 → {(newBaseStats != null ? newBaseStats.name : "null")}", this);
        }
    }
}
