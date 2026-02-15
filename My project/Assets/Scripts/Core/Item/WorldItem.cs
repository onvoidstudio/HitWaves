using UnityEngine;

namespace HitWaves.Core.Item
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class WorldItem : MonoBehaviour
    {
        private const string LOG_TAG = "WorldItem";

        [SerializeField] private ItemMaker _itemData;
        [SerializeField] private LayerMask _pickupLayer;

        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private bool _isPickedUp;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            if (_rigidbody != null)
            {
                _rigidbody.gravityScale = 0f;
                _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rigidbody.linearDamping = 5f;
            }

            if (_collider != null && !_collider.isTrigger)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: Collider2D의 isTrigger가 false. OnTriggerEnter2D가 동작하지 않습니다.", this);
            }

            if (_itemData == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: ItemMaker가 할당되지 않음.", this);
                return;
            }

            if (_spriteRenderer != null && _itemData.Icon != null)
            {
                _spriteRenderer.sprite = _itemData.Icon;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPickedUp) return;
            if (_itemData == null) return;
            if (((1 << other.gameObject.layer) & _pickupLayer) == 0) return;

            if (!TryPickup(other))
            {
                DisableTrigger();
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (_isPickedUp) return;
            if (((1 << collision.gameObject.layer) & _pickupLayer) == 0) return;

            EnableTrigger();
        }

        private bool TryPickup(Collider2D other)
        {
            StatHandler statHandler = other.GetComponentInParent<StatHandler>();
            if (statHandler == null) return false;

            switch (_itemData.Type)
            {
                case ItemType.Absorb:
                    _isPickedUp = true;
                    ApplyStatModifiers(statHandler);
                    ApplyInstantEffects(statHandler);
                    ItemHistory history = statHandler.GetComponent<ItemHistory>();
                    if (history != null)
                    {
                        history.AddAbsorbed(_itemData);
                    }
                    DebugLogger.Log(LOG_TAG, $"{statHandler.gameObject.name}이(가) '{_itemData.ItemName}' 획득 (흡수)", this);
                    Destroy(gameObject);
                    return true;

                case ItemType.Equipment:
                    EquipmentHandler equipHandler = statHandler.GetComponent<EquipmentHandler>();
                    if (equipHandler == null) return false;
                    if (equipHandler.TryEquip(_itemData))
                    {
                        _isPickedUp = true;
                        DebugLogger.Log(LOG_TAG, $"{statHandler.gameObject.name}이(가) '{_itemData.ItemName}' 장착", this);
                        Destroy(gameObject);
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private void DisableTrigger()
        {
            if (_collider != null)
            {
                _collider.isTrigger = false;
            }
        }

        private void EnableTrigger()
        {
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        public void Initialize(ItemMaker data)
        {
            _itemData = data;
            _isPickedUp = false;

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer != null && _itemData != null && _itemData.Icon != null)
            {
                _spriteRenderer.sprite = _itemData.Icon;
            }
        }

        private void ApplyStatModifiers(StatHandler statHandler)
        {
            if (_itemData.StatModifiers == null || _itemData.StatModifiers.Count == 0) return;

            foreach (StatModifierEntry entry in _itemData.StatModifiers)
            {
                statHandler.AddModifier(entry.statType, entry.amount);
            }
        }

        private void ApplyInstantEffects(StatHandler statHandler)
        {
            if (_itemData.InstantEffects == null || _itemData.InstantEffects.Count == 0) return;

            HealthHandler healthHandler = statHandler.GetComponent<HealthHandler>();

            foreach (InstantEffectEntry entry in _itemData.InstantEffects)
            {
                switch (entry.effectType)
                {
                    case InstantEffectType.HealFlat:
                        if (healthHandler != null)
                        {
                            healthHandler.Heal(entry.value);
                        }
                        break;

                    case InstantEffectType.HealFull:
                        if (healthHandler != null)
                        {
                            float maxHealth = statHandler.GetStat(StatType.MaxHealth);
                            healthHandler.Heal(maxHealth);
                        }
                        break;
                }
            }
        }
    }
}
