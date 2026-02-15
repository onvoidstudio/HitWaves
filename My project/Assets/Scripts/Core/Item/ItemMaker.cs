using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Item
{
    [CreateAssetMenu(fileName = "New Item Data", menuName = "HitWaves/Item Maker")]
    public class ItemMaker : ScriptableObject
    {
        [Header("공통")]
        [SerializeField] private string _itemName;
        [SerializeField] private string _description;
        [SerializeField] private ItemType _itemType;
        [SerializeField] private Sprite _icon;

        [Header("스탯 변경 (흡수: 영구, 장착: 장착 중)")]
        [SerializeField] private List<StatModifierEntry> _statModifiers;

        [Header("흡수형 전용")]
        [SerializeField] private List<InstantEffectEntry> _instantEffects;

        [Header("장착형 전용")]
        [SerializeField] private EquipmentSlotType _slotType;
        [SerializeField] private float _weight;

        public string ItemName => _itemName;
        public string Description => _description;
        public ItemType Type => _itemType;
        public Sprite Icon => _icon;
        public IReadOnlyList<StatModifierEntry> StatModifiers => _statModifiers;
        public IReadOnlyList<InstantEffectEntry> InstantEffects => _instantEffects;
        public string SlotType => _slotType.ToString();
        public float Weight => _weight;
    }
}
