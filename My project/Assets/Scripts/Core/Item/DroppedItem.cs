using UnityEngine;
using HitWaves.Entity.Player;

namespace HitWaves.Core.Item
{
    /// <summary>
    /// 월드에 드롭된 아이템.
    /// 아이콘 스프라이트로 표시, 물리 상호작용(발로 차기), 공격 시 내구도 감소.
    /// 밟으면 빈 슬롯에 자동 줍기.
    /// </summary>
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(HealthHandler))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class DroppedItem : MonoBehaviour
    {
        private const string LOG_TAG = "DroppedItem";

        private ItemInstance _item;
        private HealthHandler _healthHandler;

        public ItemInstance Item => _item;

        /// <summary>
        /// 드롭 아이템을 초기화한다. Create()에서 호출.
        /// </summary>
        public void Initialize(ItemInstance item)
        {
            _item = item;

            StatHandler statHandler = GetComponent<StatHandler>();
            _healthHandler = GetComponent<HealthHandler>();
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            // 진영: Object
            statHandler.SetFaction(Faction.Object);

            // 체력 = 아이템 현재 내구도, 견고함 = 아이템 견고함
            float durability = item.IsIndestructible ? 9999f : item.CurrentDurability;
            statHandler.AddModifier(StatType.MaxHealth, durability);

            if (item.Data != null)
            {
                statHandler.AddModifier(StatType.Toughness, item.Data.Toughness);
            }

            // 아이콘 스프라이트 표시
            if (item.Data != null && item.Data.Icon != null)
            {
                spriteRenderer.sprite = item.Data.Icon;
            }

            // 사망(내구도 0) 시 파괴
            _healthHandler.OnDeath += HandleDeath;

            DebugLogger.Log(LOG_TAG,
                $"드롭 아이템 생성 — {item.Data?.ItemName ?? "Unknown"}, " +
                $"내구도: {durability}", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 트리거 콜라이더 = 줍기 범위
            Inventory inventory = other.GetComponent<Inventory>();
            if (inventory == null) return;

            // 내구도 동기화 후 줍기 시도
            ItemInstance pickedItem = PickUp();
            if (inventory.TryPickUp(pickedItem))
            {
                DebugLogger.Log(LOG_TAG,
                    $"아이템 줍기 — {_item.Data?.ItemName ?? "Unknown"}", this);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 아이템을 회수한다. HealthHandler 체력을 ItemInstance 내구도에 동기화.
        /// </summary>
        public ItemInstance PickUp()
        {
            if (_item != null && !_item.IsIndestructible)
            {
                _item.SetDurability(_healthHandler.CurrentHealth);
            }

            _healthHandler.OnDeath -= HandleDeath;
            return _item;
        }

        private void HandleDeath()
        {
            DebugLogger.Log(LOG_TAG,
                $"드롭 아이템 파괴 — {_item?.Data?.ItemName ?? "Unknown"}", this);
            Destroy(gameObject);
        }

        /// <summary>
        /// 드롭 아이템을 월드에 생성한다 (팩토리 메서드).
        /// </summary>
        public static DroppedItem Create(ItemInstance item, Vector2 position)
        {
            if (item == null || item.Data == null) return null;

            GameObject go = new GameObject($"DroppedItem_{item.Data.ItemName}");
            go.transform.position = position;

            // 스프라이트
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;

            // 물리 (탑다운, 중력 없음)
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 5f;
            rb.angularDamping = 3f;
            rb.mass = 0.5f;

            // 물리 콜라이더 (투사체 충돌 + 발로 차기)
            BoxCollider2D physicsCollider = go.AddComponent<BoxCollider2D>();
            physicsCollider.size = new Vector2(0.5f, 0.5f);

            // 줍기 콜라이더 (트리거)
            CircleCollider2D pickupCollider = go.AddComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = 0.8f;

            // StatHandler + HealthHandler (Awake에서 초기화)
            go.AddComponent<StatHandler>();
            go.AddComponent<HealthHandler>();

            // DroppedItem 초기화
            DroppedItem dropped = go.AddComponent<DroppedItem>();
            dropped.Initialize(item);

            return dropped;
        }
    }
}
