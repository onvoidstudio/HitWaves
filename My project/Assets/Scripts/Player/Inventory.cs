using System;
using System.Collections.Generic;
using UnityEngine;
using HitWaves.Core;
using HitWaves.Core.Item;

namespace HitWaves.Entity.Player
{
    /// <summary>
    /// 플레이어 인벤토리. 장비 슬롯 목록을 관리한다.
    /// 장착/해제 시 StatModifier를 StatHandler에 자동 적용/제거.
    /// 초기 상태: 오른손 + 왼손 = 2슬롯.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        private const string LOG_TAG = "Inventory";

        [Header("시작 장비")]
        [Tooltip("게임 시작 시 오른손에 자동 장착할 무기")]
        [SerializeField] private ItemData _startingWeapon;

        private readonly List<EquipmentSlot> _slots = new List<EquipmentSlot>();

        private int _activeHandIndex;
        private StatHandler _statHandler;

        public IReadOnlyList<EquipmentSlot> Slots => _slots;
        public EquipmentSlot ActiveHand => _slots.Count > 0 ? _slots[_activeHandIndex] : null;
        public int ActiveHandIndex => _activeHandIndex;

        /// <summary>
        /// 슬롯 내용이 변경될 때 발생 (장착/해제/교체).
        /// </summary>
        public event Action<EquipmentSlot> OnSlotChanged;

        /// <summary>
        /// 활성 손이 전환될 때 발생.
        /// </summary>
        public event Action<int> OnActiveHandChanged;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
            InitializeDefaultSlots();
            EquipStartingWeapon();
        }

        /// <summary>
        /// 시작 무기를 오른손에 자동 장착한다.
        /// </summary>
        private void EquipStartingWeapon()
        {
            if (_startingWeapon == null) return;

            ItemInstance weapon = new ItemInstance(_startingWeapon);
            EquipmentSlot rightHand = GetSlot(EquipmentSlotType.RightHand);
            if (rightHand != null)
            {
                EquipToSlot(rightHand, weapon);
                DebugLogger.Log(LOG_TAG,
                    $"시작 무기 장착 — {_startingWeapon.ItemName}", this);
            }
        }

        private void InitializeDefaultSlots()
        {
            AddSlot(EquipmentSlotType.RightHand);
            AddSlot(EquipmentSlotType.LeftHand);
            _activeHandIndex = 0;

            DebugLogger.Log(LOG_TAG,
                $"기본 슬롯 초기화 — {_slots.Count}개 (RightHand, LeftHand)", this);
        }

        /// <summary>
        /// 슬롯을 추가한다 (가방/벨트 장착 시 호출).
        /// </summary>
        public EquipmentSlot AddSlot(EquipmentSlotType slotType)
        {
            EquipmentSlot slot = new EquipmentSlot(slotType);
            slot.OnChanged += HandleSlotChanged;
            _slots.Add(slot);
            return slot;
        }

        /// <summary>
        /// 슬롯을 제거한다 (장비 해제로 슬롯 소실 시).
        /// 슬롯에 아이템이 있으면 반환한다.
        /// </summary>
        public ItemInstance RemoveSlot(EquipmentSlot slot)
        {
            ItemInstance droppedItem = slot.EquippedItem;
            if (droppedItem != null)
            {
                RemoveStatModifiers(droppedItem);
                UnsubscribeBroken(droppedItem);
                slot.Unequip();
            }
            slot.OnChanged -= HandleSlotChanged;
            _slots.Remove(slot);

            if (_activeHandIndex >= _slots.Count)
            {
                _activeHandIndex = Mathf.Max(0, _slots.Count - 1);
            }

            return droppedItem;
        }

        /// <summary>
        /// 활성 손을 전환한다 (F키).
        /// 손 슬롯(RightHand, LeftHand)만 순환한다.
        /// </summary>
        public void SwitchActiveHand()
        {
            List<int> handIndices = new List<int>();
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].SlotType == EquipmentSlotType.RightHand ||
                    _slots[i].SlotType == EquipmentSlotType.LeftHand)
                {
                    handIndices.Add(i);
                }
            }

            if (handIndices.Count <= 1) return;

            int currentPos = handIndices.IndexOf(_activeHandIndex);
            int nextPos = (currentPos + 1) % handIndices.Count;
            _activeHandIndex = handIndices[nextPos];

            OnActiveHandChanged?.Invoke(_activeHandIndex);

            DebugLogger.Log(LOG_TAG,
                $"활성 손 전환 — {_slots[_activeHandIndex].SlotType}", this);
        }

        /// <summary>
        /// 특정 타입의 슬롯을 찾는다.
        /// </summary>
        public EquipmentSlot GetSlot(EquipmentSlotType slotType)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].SlotType == slotType) return _slots[i];
            }
            return null;
        }

        /// <summary>
        /// 활성 손에 아이템을 장착한다.
        /// 기존 아이템은 스탯 해제 후 반환.
        /// </summary>
        public ItemInstance EquipToActiveHand(ItemInstance item)
        {
            if (ActiveHand == null) return item;
            return EquipToSlot(ActiveHand, item);
        }

        /// <summary>
        /// 특정 슬롯에 아이템을 장착한다.
        /// 기존 아이템 스탯 해제 → 새 아이템 스탯 적용.
        /// </summary>
        public ItemInstance EquipToSlot(EquipmentSlot slot, ItemInstance item)
        {
            // 기존 아이템 해제
            ItemInstance previous = slot.EquippedItem;
            if (previous != null)
            {
                RemoveStatModifiers(previous);
                UnsubscribeBroken(previous);
            }

            // 새 아이템 장착
            slot.Equip(item);

            if (item != null)
            {
                ApplyStatModifiers(item);
                SubscribeBroken(item, slot);

                DebugLogger.Log(LOG_TAG,
                    $"장착 — [{slot.SlotType}] {item.Data.ItemName}", this);
            }

            return previous;
        }

        /// <summary>
        /// 두 슬롯의 아이템을 교환한다 (드래그 앤 드롭).
        /// </summary>
        public void SwapSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _slots.Count) return;
            if (indexB < 0 || indexB >= _slots.Count) return;
            if (indexA == indexB) return;

            EquipmentSlot slotA = _slots[indexA];
            EquipmentSlot slotB = _slots[indexB];

            ItemInstance itemA = slotA.EquippedItem;
            ItemInstance itemB = slotB.EquippedItem;

            // 기존 스탯 해제
            if (itemA != null) { RemoveStatModifiers(itemA); UnsubscribeBroken(itemA); }
            if (itemB != null) { RemoveStatModifiers(itemB); UnsubscribeBroken(itemB); }

            // 교환
            slotA.Equip(itemB);
            slotB.Equip(itemA);

            // 새 위치에 스탯 재적용
            if (itemA != null) { ApplyStatModifiers(itemA); SubscribeBroken(itemA, slotB); }
            if (itemB != null) { ApplyStatModifiers(itemB); SubscribeBroken(itemB, slotA); }

            DebugLogger.Log(LOG_TAG,
                $"슬롯 교환 — [{slotA.SlotType}] ↔ [{slotB.SlotType}]", this);
        }

        /// <summary>
        /// 슬롯에서 아이템을 꺼낸다 (월드에 드롭 시).
        /// 스탯 해제 후 아이템을 반환한다.
        /// </summary>
        public ItemInstance DropFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return null;

            EquipmentSlot slot = _slots[slotIndex];
            if (slot.IsEmpty) return null;

            ItemInstance item = slot.EquippedItem;
            RemoveStatModifiers(item);
            UnsubscribeBroken(item);
            slot.Unequip();

            DebugLogger.Log(LOG_TAG,
                $"아이템 드롭 — [{slot.SlotType}] {item.Data.ItemName}", this);

            return item;
        }

        /// <summary>
        /// 슬롯의 인덱스를 반환한다.
        /// </summary>
        public int GetSlotIndex(EquipmentSlot slot)
        {
            return _slots.IndexOf(slot);
        }

        /// <summary>
        /// 빈 슬롯에 아이템을 장착한다 (줍기).
        /// 빈 슬롯이 없으면 false 반환.
        /// </summary>
        public bool TryPickUp(ItemInstance item)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    EquipToSlot(_slots[i], item);
                    DebugLogger.Log(LOG_TAG,
                        $"아이템 줍기 — [{_slots[i].SlotType}] {item.Data.ItemName}", this);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 모든 슬롯을 초기화한다 (런 리셋).
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                ItemInstance item = _slots[i].EquippedItem;
                if (item != null)
                {
                    RemoveStatModifiers(item);
                    UnsubscribeBroken(item);
                }
                _slots[i].Clear();
            }

            DebugLogger.Log(LOG_TAG, "모든 슬롯 초기화", this);
        }

        /// <summary>
        /// 아이템의 StatModifier를 본체 StatHandler에 적용한다.
        /// </summary>
        private void ApplyStatModifiers(ItemInstance item)
        {
            if (_statHandler == null || item.Data == null) return;

            StatModifier[] modifiers = item.Data.StatModifiers;
            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Length; i++)
            {
                _statHandler.AddModifier(modifiers[i].statType, modifiers[i].value);
            }
        }

        /// <summary>
        /// 아이템의 StatModifier를 본체 StatHandler에서 제거한다.
        /// </summary>
        private void RemoveStatModifiers(ItemInstance item)
        {
            if (_statHandler == null || item.Data == null) return;

            StatModifier[] modifiers = item.Data.StatModifiers;
            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Length; i++)
            {
                _statHandler.RemoveModifier(modifiers[i].statType, modifiers[i].value);
            }
        }

        /// <summary>
        /// 아이템 파괴 이벤트를 구독한다.
        /// </summary>
        private void SubscribeBroken(ItemInstance item, EquipmentSlot slot)
        {
            item.OnBroken += (broken) => HandleItemBroken(broken, slot);
        }

        private void UnsubscribeBroken(ItemInstance item)
        {
            // ItemInstance는 일회용이므로 이벤트 구독자를 null로 초기화
            item.OnBroken = null;
        }

        /// <summary>
        /// 장비 파괴 시 슬롯에서 자동 제거 + 스탯 해제.
        /// </summary>
        private void HandleItemBroken(ItemInstance item, EquipmentSlot slot)
        {
            RemoveStatModifiers(item);
            slot.Clear();

            DebugLogger.Log(LOG_TAG,
                $"장비 파괴 — [{slot.SlotType}] {item.Data.ItemName} 슬롯에서 제거됨", this);
        }

        private void HandleSlotChanged(EquipmentSlot slot)
        {
            OnSlotChanged?.Invoke(slot);
        }
    }
}
