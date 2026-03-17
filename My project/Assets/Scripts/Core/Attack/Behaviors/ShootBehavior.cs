using UnityEngine;
using HitWaves.Core.Item;
using HitWaves.Entity.Player;

namespace HitWaves.Core.Attack.Behaviors
{
    /// <summary>
    /// 원거리 공격. WeaponData의 스탯으로 투사체를 발사한다.
    /// </summary>
    public class ShootBehavior : IAttackBehavior
    {
        private const string LOG_TAG = "ShootBehavior";

        private WeaponData _weaponData;
        private Inventory _inventory;
        private bool _isReloading;
        private float _reloadEndTime;

        public bool CooldownOnExecute => true;
        public bool IsReloading => _isReloading;

        public void Initialize(AttackHandler handler)
        {
            _inventory = handler.GetComponent<Inventory>();
            DebugLogger.Log(LOG_TAG, "ShootBehavior 초기화", handler);
        }

        /// <summary>
        /// WeaponData를 갱신한다 (무기 교체 시 호출).
        /// </summary>
        public void SetWeaponData(WeaponData data)
        {
            _weaponData = data;
        }

        public int Execute(AttackHandler handler, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return 0;
            if (_weaponData == null) return 0;
            if (_weaponData.ProjectilePrefab == null)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    $"{_weaponData.ItemName}: 투사체 프리팹 미설정", handler);
                return 0;
            }

            // 재장전 체크
            if (_isReloading)
            {
                if (Time.time >= _reloadEndTime)
                {
                    FinishReload();
                }
                else
                {
                    return 0; // 재장전 중
                }
            }

            // 탄약 체크
            ItemInstance activeItem = GetActiveItemInstance();
            if (activeItem != null && activeItem.IsAmmoEmpty())
            {
                StartReload();
                return 0;
            }

            direction = direction.normalized;
            Vector2 spawnPos = (Vector2)handler.transform.position + direction * 0.5f;

            // 투사체 스폰
            GameObject projGo = Object.Instantiate(
                _weaponData.ProjectilePrefab, spawnPos, Quaternion.identity);

            // 크기 적용
            float size = _weaponData.ProjectileSize;
            projGo.transform.localScale *= size;

            // Projectile 컴포넌트 초기화
            DebugLogger.Log(LOG_TAG,
                $"스폰 완료 — projGo={projGo.name}, active={projGo.activeSelf}, " +
                $"components={projGo.GetComponents<Component>().Length}", handler);

            Projectile projectile = projGo.GetComponent<Projectile>();
            if (projectile == null)
            {
                DebugLogger.LogWarning(LOG_TAG, "프리팹에 Projectile 없음, 런타임 추가", handler);
                projectile = projGo.AddComponent<Projectile>();
            }

            if (projectile == null)
            {
                DebugLogger.LogError(LOG_TAG,
                    $"Projectile 추가 실패! projGo destroyed={projGo == null}", handler);
                if (projGo != null) Object.Destroy(projGo);
                return 0;
            }

            StatHandler attackerStats = handler.GetComponent<StatHandler>();
            Faction ownerFaction = attackerStats != null ? attackerStats.Faction : Faction.Player;
            Collider2D shooterCollider = handler.GetComponent<Collider2D>();

            projectile.Initialize(
                direction,
                _weaponData.ProjectileSpeed,
                _weaponData.Damage,
                _weaponData.KnockbackForce,
                _weaponData.Range,
                ownerFaction,
                _weaponData.PierceCount,
                shooterCollider);

            // 탄약 소모
            if (activeItem != null)
            {
                activeItem.UseAmmo();

                // 탄창 비면 자동 재장전
                if (activeItem.IsAmmoEmpty())
                {
                    StartReload();
                }
            }

            DebugLogger.Log(LOG_TAG,
                $"발사 — {_weaponData.ItemName}, dir:{direction}, " +
                $"spd:{_weaponData.ProjectileSpeed}, dmg:{_weaponData.Damage}" +
                (activeItem != null ? $", ammo:{activeItem.CurrentAmmo}" : ""), handler);

            return 0; // 히트는 투사체가 충돌 시 처리
        }

        private ItemInstance GetActiveItemInstance()
        {
            if (_inventory == null || _inventory.ActiveHand == null) return null;
            return _inventory.ActiveHand.EquippedItem;
        }

        private void StartReload()
        {
            if (_isReloading) return;
            if (_weaponData == null) return;

            _isReloading = true;
            _reloadEndTime = Time.time + _weaponData.ReloadTime;

            DebugLogger.Log(LOG_TAG,
                $"재장전 시작 — {_weaponData.ItemName}, {_weaponData.ReloadTime}초");
        }

        private void FinishReload()
        {
            _isReloading = false;

            ItemInstance activeItem = GetActiveItemInstance();
            if (activeItem != null)
            {
                activeItem.Reload();
            }
        }

        public void Cleanup() { }
    }
}
