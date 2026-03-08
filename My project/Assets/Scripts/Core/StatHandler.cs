using System;
using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core
{
    public class StatHandler : MonoBehaviour
    {
        private const string LOG_TAG = "StatHandler";

        [Header("Base Stats")]
        [Tooltip("이 엔티티의 기본 스탯 데이터 (ScriptableObject)")]
        [SerializeField] private EntityStatData _baseStats;

        [Header("진영")]
        [Tooltip("이 엔티티의 소속 진영 (Player/Enemy/Neutral)")]
        [SerializeField] private Faction _faction = Faction.Enemy;

        private Dictionary<StatType, float> _statModifiers;

        public Faction Faction => _faction;

        public void SetFaction(Faction newFaction)
        {
            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: 진영 변경 {_faction} → {newFaction}", this);
            _faction = newFaction;
        }

        public event Action<StatType, float> OnStatChanged;

        private void Awake()
        {
            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: Awake 시작 — Faction: {_faction}", this);

            InitializeModifiers();

            if (_baseStats == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: baseStats가 할당되지 않음. 기본값 0 사용.", this);
            }
            else
            {
                DebugLogger.Log(LOG_TAG, $"{gameObject.name}: 초기화 완료 — baseStats: {_baseStats.name}", this);
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

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: modifier 초기화 완료 — {_statModifiers.Count}개 스탯", this);
        }

        public float GetStat(StatType statType)
        {
            InitializeModifiers();

            float baseValue = _baseStats != null ? _baseStats.GetStat(statType) : 0f;
            float modifier = _statModifiers.TryGetValue(statType, out float mod) ? mod : 0f;
            return baseValue + modifier;
        }

        public void AddModifier(StatType statType, float amount)
        {
            InitializeModifiers();

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
            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: {statType} modifier -{amount}", this);
            AddModifier(statType, -amount);
        }

        public void SetBaseStats(EntityStatData newBaseStats)
        {
            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: baseStats 변경 → {(newBaseStats != null ? newBaseStats.name : "null")}", this);
            _baseStats = newBaseStats;
        }
    }
}
