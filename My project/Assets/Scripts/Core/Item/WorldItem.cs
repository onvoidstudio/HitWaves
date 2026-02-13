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
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_itemData == null) return;

            if (((1 << other.gameObject.layer) & _pickupLayer) == 0) return;

            StatHandler statHandler = other.GetComponent<StatHandler>();
            if (statHandler == null) return;

            if (_itemData.Type == ItemType.Absorb)
            {
                ApplyStatModifiers(statHandler);
                ApplyInstantEffects(other.gameObject);
            }

            DebugLogger.Log(LOG_TAG, $"{other.gameObject.name}이(가) '{_itemData.ItemName}' 획득", this);
            Destroy(gameObject);
        }

        private void ApplyStatModifiers(StatHandler statHandler)
        {
            foreach (StatModifierEntry entry in _itemData.StatModifiers)
            {
                statHandler.AddModifier(entry.statType, entry.amount);
            }
        }

        private void ApplyInstantEffects(GameObject target)
        {
            if (_itemData.InstantEffects == null || _itemData.InstantEffects.Count == 0) return;

            HealthHandler healthHandler = target.GetComponent<HealthHandler>();

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
                            float maxHealth = target.GetComponent<StatHandler>().GetStat(StatType.MaxHealth);
                            healthHandler.Heal(maxHealth);
                        }
                        break;
                }
            }
        }
    }
}
