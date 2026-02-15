using System;
using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Item
{
    [RequireComponent(typeof(StatHandler))]
    public class EquipmentHandler : MonoBehaviour
    {
        private const string LOG_TAG = "EquipmentHandler";

        [SerializeField] private List<EquipmentSlotType> _defaultSlots = new List<EquipmentSlotType>
        {
            EquipmentSlotType.Head,
            EquipmentSlotType.Body,
            EquipmentSlotType.Arms,
            EquipmentSlotType.Legs,
            EquipmentSlotType.Back,
            EquipmentSlotType.Accessory,
            EquipmentSlotType.Weapon
        };
        [SerializeField] private WorldItem _dropPrefab;
        [SerializeField] private float _weightPerStrength = 10f;

        private StatHandler _statHandler;
        private HashSet<string> _availableSlots;
        private Dictionary<string, ItemMaker> _equippedItems;
        private float _currentWeight;

        public event Action<string, ItemMaker> OnEquipped;
        public event Action<string, ItemMaker> OnUnequipped;

        public IReadOnlyCollection<string> AvailableSlots => _availableSlots;
        public float CurrentWeight => _currentWeight;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
            _availableSlots = new HashSet<string>();
            foreach (EquipmentSlotType slot in _defaultSlots)
            {
                _availableSlots.Add(slot.ToString());
            }
            _equippedItems = new Dictionary<string, ItemMaker>();
            _currentWeight = 0f;
        }

        public float GetMaxWeight()
        {
            return _statHandler.GetStat(StatType.Strength) * _weightPerStrength;
        }

        public bool CanEquip(ItemMaker itemData)
        {
            if (itemData == null) return false;
            if (itemData.Type != ItemType.Equipment) return false;
            if (!_availableSlots.Contains(itemData.SlotType)) return false;

            float weightAfter = _currentWeight + itemData.Weight;
            if (_equippedItems.TryGetValue(itemData.SlotType, out ItemMaker existingItem))
            {
                weightAfter -= existingItem.Weight;
            }

            return weightAfter <= GetMaxWeight();
        }

        public bool TryEquip(ItemMaker itemData)
        {
            if (itemData == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: null ItemMaker 장착 시도.", this);
                return false;
            }

            if (itemData.Type != ItemType.Equipment)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: '{itemData.ItemName}'은(는) Equipment 타입이 아님.", this);
                return false;
            }

            if (!_availableSlots.Contains(itemData.SlotType))
            {
                DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{itemData.SlotType}' 슬롯이 없음. '{itemData.ItemName}' 장착 불가.", this);
                return false;
            }

            float weightAfter = _currentWeight + itemData.Weight;
            ItemMaker existingItem = null;
            if (_equippedItems.TryGetValue(itemData.SlotType, out existingItem))
            {
                weightAfter -= existingItem.Weight;
            }

            if (weightAfter > GetMaxWeight())
            {
                DebugLogger.Log(LOG_TAG, $"{gameObject.name}: 무게 초과. '{itemData.ItemName}' 장착 불가. (현재: {_currentWeight}, 추가: {itemData.Weight}, 최대: {GetMaxWeight()})", this);
                return false;
            }

            if (existingItem != null)
            {
                UnequipInternal(itemData.SlotType);
                DropEquipment(existingItem);
            }

            _equippedItems[itemData.SlotType] = itemData;
            _currentWeight += itemData.Weight;

            if (itemData.StatModifiers != null)
            {
                foreach (StatModifierEntry entry in itemData.StatModifiers)
                {
                    _statHandler.AddModifier(entry.statType, entry.amount);
                }
            }

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{itemData.ItemName}' 장착 ({itemData.SlotType}). 무게: {_currentWeight}/{GetMaxWeight()}", this);
            OnEquipped?.Invoke(itemData.SlotType, itemData);
            return true;
        }

        public ItemMaker Unequip(string slotType)
        {
            return UnequipInternal(slotType);
        }

        private ItemMaker UnequipInternal(string slotType)
        {
            if (!_equippedItems.TryGetValue(slotType, out ItemMaker itemData))
            {
                DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{slotType}' 슬롯에 장착된 장비 없음.", this);
                return null;
            }

            _equippedItems.Remove(slotType);
            _currentWeight -= itemData.Weight;

            if (itemData.StatModifiers != null)
            {
                foreach (StatModifierEntry entry in itemData.StatModifiers)
                {
                    _statHandler.RemoveModifier(entry.statType, entry.amount);
                }
            }

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{itemData.ItemName}' 해제 ({slotType}). 무게: {_currentWeight}/{GetMaxWeight()}", this);
            OnUnequipped?.Invoke(slotType, itemData);
            return itemData;
        }

        public void DropEquipment(ItemMaker itemData)
        {
            if (itemData == null) return;

            if (_dropPrefab == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: _dropPrefab이 할당되지 않음. 장비 드롭 불가.", this);
                return;
            }

            WorldItem dropped = Instantiate(_dropPrefab, transform.position, Quaternion.identity);
            dropped.Initialize(itemData);

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{itemData.ItemName}' 드롭.", this);
        }

        public void AddSlot(string slotType)
        {
            if (_availableSlots.Add(slotType))
            {
                DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{slotType}' 슬롯 추가.", this);
            }
        }

        public void RemoveSlot(string slotType)
        {
            if (!_availableSlots.Remove(slotType)) return;

            DebugLogger.Log(LOG_TAG, $"{gameObject.name}: '{slotType}' 슬롯 제거.", this);

            if (_equippedItems.ContainsKey(slotType))
            {
                ItemMaker itemData = UnequipInternal(slotType);
                DropEquipment(itemData);
            }
        }

        public ItemMaker GetEquipped(string slotType)
        {
            _equippedItems.TryGetValue(slotType, out ItemMaker itemData);
            return itemData;
        }
    }
}
