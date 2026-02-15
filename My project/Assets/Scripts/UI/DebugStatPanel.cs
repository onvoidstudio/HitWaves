using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using HitWaves.Core;
using HitWaves.Core.Item;

namespace HitWaves.UI
{
    public class DebugStatPanel : MonoBehaviour
    {
        private const string LOG_TAG = "DebugStatPanel";

        [Header("UI")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _displayText;

        [Header("Player")]
        [SerializeField] private StatHandler _statHandler;
        [SerializeField] private HealthHandler _healthHandler;
        [SerializeField] private EquipmentHandler _equipmentHandler;
        [SerializeField] private ItemHistory _itemHistory;

        private bool _isOpen;
        private StringBuilder _sb;

        private void Awake()
        {
            _sb = new StringBuilder(1024);

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            _isOpen = false;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current[Key.Backquote].wasPressedThisFrame) return;

            Toggle();
        }

        private void Toggle()
        {
            _isOpen = !_isOpen;

            if (_panel != null)
            {
                _panel.SetActive(_isOpen);
            }

            if (_isOpen)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_displayText == null) return;

            _sb.Clear();

            BuildStats();
            _sb.AppendLine();
            BuildEquipment();
            _sb.AppendLine();
            BuildPassives();

            _displayText.text = _sb.ToString();
        }

        private void BuildStats()
        {
            _sb.AppendLine("<b>[Stats]</b>");

            if (_statHandler == null)
            {
                _sb.AppendLine("No StatHandler");
                return;
            }

            if (_healthHandler != null)
            {
                float currentHp = _healthHandler.CurrentHealth;
                float maxHp = _statHandler.GetStat(StatType.MaxHealth);
                _sb.AppendLine($"HP: {currentHp} / {maxHp}");
            }

            AppendStat(StatType.MoveSpeed, "MoveSpeed");
            AppendStat(StatType.Damage, "Damage");
            AppendStat(StatType.AttackSpeed, "AtkSpeed");
            AppendStat(StatType.Strength, "Strength");
            AppendStat(StatType.MaxHitCount, "MaxHit");
            AppendStat(StatType.InvincibilityDuration, "I-Frame");
            AppendStat(StatType.MaxHealth, "MaxHP");
            AppendStat(StatType.MaxStamina, "MaxStamina");
            AppendStat(StatType.MaxMental, "MaxMental");
            AppendStat(StatType.JumpRange, "JumpRange");
            AppendStat(StatType.FlightDuration, "FlightDur");
            AppendStat(StatType.HealthRegenRate, "HP Regen");
            AppendStat(StatType.HealthRegenDelay, "HP RegenDelay");
            AppendStat(StatType.HealthRegenInterval, "HP RegenInterval");
            AppendStat(StatType.StaminaRegenRate, "ST Regen");
            AppendStat(StatType.StaminaRegenDelay, "ST RegenDelay");
            AppendStat(StatType.StaminaRegenInterval, "ST RegenInterval");
        }

        private void AppendStat(StatType statType, string displayName)
        {
            float value = _statHandler.GetStat(statType);
            _sb.AppendLine($"{displayName}: {value}");
        }

        private void BuildEquipment()
        {
            _sb.AppendLine("<b>[Equipment]</b>");

            if (_equipmentHandler == null)
            {
                _sb.AppendLine("No EquipmentHandler");
                return;
            }

            foreach (string slot in _equipmentHandler.AvailableSlots)
            {
                ItemMaker equipped = _equipmentHandler.GetEquipped(slot);
                string itemName = equipped != null ? equipped.ItemName : "-";
                _sb.AppendLine($"{slot}: {itemName}");
            }

            _sb.AppendLine($"Weight: {_equipmentHandler.CurrentWeight} / {_equipmentHandler.GetMaxWeight()}");
        }

        private void BuildPassives()
        {
            _sb.AppendLine("<b>[Passives]</b>");

            if (_itemHistory == null || _itemHistory.AbsorbedItems.Count == 0)
            {
                _sb.AppendLine("None");
                return;
            }

            foreach (ItemMaker item in _itemHistory.AbsorbedItems)
            {
                _sb.Append($"- {item.ItemName}");

                if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                {
                    _sb.Append(" (");
                    for (int i = 0; i < item.StatModifiers.Count; i++)
                    {
                        StatModifierEntry mod = item.StatModifiers[i];
                        string sign = mod.amount >= 0 ? "+" : "";
                        _sb.Append($"{mod.statType} {sign}{mod.amount}");
                        if (i < item.StatModifiers.Count - 1)
                        {
                            _sb.Append(", ");
                        }
                    }
                    _sb.Append(")");
                }

                _sb.AppendLine();
            }
        }

        private void SubscribeEvents()
        {
            if (_statHandler != null)
            {
                _statHandler.OnStatChanged += HandleStatChanged;
            }
            if (_healthHandler != null)
            {
                _healthHandler.OnHealthChanged += HandleHealthChanged;
            }
            if (_equipmentHandler != null)
            {
                _equipmentHandler.OnEquipped += HandleEquipmentChanged;
                _equipmentHandler.OnUnequipped += HandleEquipmentChanged;
            }
            if (_itemHistory != null)
            {
                _itemHistory.OnItemAbsorbed += HandleItemAbsorbed;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_statHandler != null)
            {
                _statHandler.OnStatChanged -= HandleStatChanged;
            }
            if (_healthHandler != null)
            {
                _healthHandler.OnHealthChanged -= HandleHealthChanged;
            }
            if (_equipmentHandler != null)
            {
                _equipmentHandler.OnEquipped -= HandleEquipmentChanged;
                _equipmentHandler.OnUnequipped -= HandleEquipmentChanged;
            }
            if (_itemHistory != null)
            {
                _itemHistory.OnItemAbsorbed -= HandleItemAbsorbed;
            }
        }

        private void HandleStatChanged(StatType statType, float value)
        {
            if (!_isOpen) return;
            Refresh();
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (!_isOpen) return;
            Refresh();
        }

        private void HandleEquipmentChanged(string slotType, ItemMaker itemData)
        {
            if (!_isOpen) return;
            Refresh();
        }

        private void HandleItemAbsorbed(ItemMaker item)
        {
            if (!_isOpen) return;
            Refresh();
        }
    }
}
