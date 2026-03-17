using UnityEngine;

namespace HitWaves.Core.Item
{
    /// <summary>
    /// 아이템 기본 정보 SO.
    /// 런타임 상태(현재 내구도 등)는 ItemInstance에서 관리.
    /// </summary>
    [CreateAssetMenu(fileName = "New ItemData", menuName = "HitWaves/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("고유 번호 (도감/DB용)")]
        [SerializeField] private int _itemNumber;

        [Tooltip("코드용 고유 식별자 (예: wpn_pistol)")]
        [SerializeField] private string _itemId;

        [Tooltip("표시 이름")]
        [SerializeField] private string _itemName;

        [Tooltip("아이템 설명")]
        [TextArea(2, 5)]
        [SerializeField] private string _description;

        [Header("비주얼")]
        [Tooltip("UI 아이콘")]
        [SerializeField] private Sprite _icon;

        [Tooltip("월드에 놓였을 때 프리팹")]
        [SerializeField] private GameObject _worldPrefab;

        [Header("분류")]
        [Tooltip("장비 카테고리")]
        [SerializeField] private ItemCategory _category;

        [Header("물리")]
        [Tooltip("무게")]
        [Min(0f)]
        [SerializeField] private float _weight;

        [Header("내구")]
        [Tooltip("최대 내구도 (0 = 파괴 불가)")]
        [Min(0f)]
        [SerializeField] private float _maxDurability = 100f;

        [Tooltip("강도 (이 수치 이하의 데미지는 내구도 감소 없음)")]
        [Min(0f)]
        [SerializeField] private float _toughness;

        [Header("능력")]
        [Tooltip("장착 시 부여되는 스탯 수정자 목록")]
        [SerializeField] private StatModifier[] _statModifiers;

        // === 프로퍼티 ===
        public int ItemNumber => _itemNumber;
        public string ItemId => _itemId;
        public string ItemName => _itemName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject WorldPrefab => _worldPrefab;
        public ItemCategory Category => _category;
        public float Weight => _weight;
        public float MaxDurability => _maxDurability;
        public float Toughness => _toughness;
        public StatModifier[] StatModifiers => _statModifiers;
        public bool IsIndestructible => _maxDurability <= 0f;
    }
}
