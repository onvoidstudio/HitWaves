using UnityEngine;

namespace HitWaves.Core.Item
{
    [RequireComponent(typeof(Collider2D))]
    public class WorldItem : MonoBehaviour
    {
        private const string LOG_TAG = "WorldItem";

        [SerializeField] private ItemData _itemData;
        [SerializeField] private LayerMask _pickupLayer;

        private SpriteRenderer _spriteRenderer;
        private bool _isPickedUp;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_itemData == null)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: ItemData가 할당되지 않음.", this);
                return;
            }

            if (_spriteRenderer != null && _itemData.Icon != null)
            {
                _spriteRenderer.sprite = _itemData.Icon;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                DebugLogger.LogWarning(LOG_TAG, $"{gameObject.name}: Collider2D의 isTrigger가 false. OnTriggerEnter2D가 동작하지 않습니다.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPickedUp) return;
            if (_itemData == null) return;

            if (((1 << other.gameObject.layer) & _pickupLayer) == 0) return;

            StatHandler statHandler = other.GetComponentInParent<StatHandler>();
            if (statHandler == null) return;

            if (_itemData.Type == ItemType.Absorb)
            {
                _isPickedUp = true;
                ApplyStatModifiers(statHandler);
                ApplyInstantEffects(statHandler);
                DebugLogger.Log(LOG_TAG, $"{statHandler.gameObject.name}이(가) '{_itemData.ItemName}' 획득", this);
                Destroy(gameObject);
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
