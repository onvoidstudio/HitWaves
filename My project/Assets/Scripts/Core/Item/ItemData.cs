using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Item
{
    [CreateAssetMenu(menuName = "HitWaves/Item Data")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string _itemName;
        [SerializeField] private string _description;
        [SerializeField] private ItemType _itemType;
        [SerializeField] private Sprite _icon;
        [SerializeField] private List<StatModifierEntry> _statModifiers;
        [SerializeField] private List<InstantEffectEntry> _instantEffects;

        public string ItemName => _itemName;
        public string Description => _description;
        public ItemType Type => _itemType;
        public Sprite Icon => _icon;
        public IReadOnlyList<StatModifierEntry> StatModifiers => _statModifiers;
        public IReadOnlyList<InstantEffectEntry> InstantEffects => _instantEffects;
    }
}
