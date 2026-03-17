using System;
using UnityEngine;
using HitWaves.Core.Item;

namespace HitWaves.Entity.Player
{
    /// <summary>
    /// 장비 슬롯 1개. 타입과 현재 장착된 아이템(ItemInstance)을 관리한다.
    /// </summary>
    [Serializable]
    public class EquipmentSlot
    {
        [SerializeField] private EquipmentSlotType _slotType;

        private ItemInstance _equippedItem;

        public EquipmentSlotType SlotType => _slotType;
        public ItemInstance EquippedItem => _equippedItem;
        public bool IsEmpty => _equippedItem == null;

        public event Action<EquipmentSlot> OnChanged;

        public EquipmentSlot(EquipmentSlotType slotType)
        {
            _slotType = slotType;
            _equippedItem = null;
        }

        /// <summary>
        /// 아이템을 장착한다. 기존 아이템이 있으면 반환한다.
        /// </summary>
        public ItemInstance Equip(ItemInstance item)
        {
            ItemInstance previous = _equippedItem;
            _equippedItem = item;
            OnChanged?.Invoke(this);
            return previous;
        }

        /// <summary>
        /// 현재 아이템을 해제하고 반환한다.
        /// </summary>
        public ItemInstance Unequip()
        {
            ItemInstance previous = _equippedItem;
            _equippedItem = null;
            OnChanged?.Invoke(this);
            return previous;
        }

        /// <summary>
        /// 슬롯을 초기화한다 (런 리셋 등).
        /// </summary>
        public void Clear()
        {
            _equippedItem = null;
            OnChanged?.Invoke(this);
        }
    }
}
