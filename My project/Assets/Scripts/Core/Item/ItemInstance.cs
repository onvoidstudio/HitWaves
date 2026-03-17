using System;
using UnityEngine;

namespace HitWaves.Core.Item
{
    /// <summary>
    /// 아이템 런타임 인스턴스.
    /// ItemData(불변)를 참조하고 현재 내구도 등 변동 상태를 관리한다.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        private const string LOG_TAG = "ItemInstance";

        [SerializeField] private ItemData _data;
        private float _currentDurability;
        private int _currentAmmo;

        public ItemData Data => _data;
        public float CurrentDurability => _currentDurability;
        public float MaxDurability => _data != null ? _data.MaxDurability : 0f;
        public bool IsBroken => !IsIndestructible && _currentDurability <= 0f;
        public bool IsIndestructible => _data != null && _data.IsIndestructible;
        public int CurrentAmmo => _currentAmmo;

        /// <summary>
        /// 내구도 변경 시 발생 (current, max).
        /// </summary>
        public Action<float, float> OnDurabilityChanged;

        /// <summary>
        /// 파괴(내구도 0) 시 발생.
        /// </summary>
        public Action<ItemInstance> OnBroken;

        /// <summary>
        /// 탄약 변경 시 발생 (current, max).
        /// </summary>
        public Action<int, int> OnAmmoChanged;

        public ItemInstance(ItemData data)
        {
            _data = data;
            _currentDurability = data != null ? data.MaxDurability : 0f;

            // 무기면 탄창 초기화
            WeaponData weapon = data as WeaponData;
            if (weapon != null)
            {
                _currentAmmo = weapon.MagazineSize > 0 ? weapon.MagazineSize : 0;
            }
        }

        /// <summary>
        /// 탄약을 1 소모한다. 무한(-1)이면 항상 true.
        /// </summary>
        public bool UseAmmo()
        {
            WeaponData weapon = _data as WeaponData;
            if (weapon == null) return false;
            if (weapon.MagazineSize < 0) return true; // 무한 탄창
            if (weapon.MagazineSize == 0) return true; // 탄창 없음 (근접)
            if (_currentAmmo <= 0) return false;

            _currentAmmo--;
            OnAmmoChanged?.Invoke(_currentAmmo, weapon.MagazineSize);
            return true;
        }

        /// <summary>
        /// 탄창을 최대로 재장전한다.
        /// </summary>
        public void Reload()
        {
            WeaponData weapon = _data as WeaponData;
            if (weapon == null || weapon.MagazineSize <= 0) return;

            _currentAmmo = weapon.MagazineSize;
            OnAmmoChanged?.Invoke(_currentAmmo, weapon.MagazineSize);

            DebugLogger.Log(LOG_TAG, $"{_data.ItemName} 재장전 완료 — {_currentAmmo}/{weapon.MagazineSize}");
        }

        /// <summary>
        /// 탄창이 비었는지 확인. 무한/근접이면 항상 false.
        /// </summary>
        public bool IsAmmoEmpty()
        {
            WeaponData weapon = _data as WeaponData;
            if (weapon == null) return false;
            if (weapon.MagazineSize <= 0) return false; // 무한 or 근접
            return _currentAmmo <= 0;
        }

        /// <summary>
        /// 아이템에 데미지를 준다.
        /// 강도(Toughness) 이하의 데미지는 무시.
        /// </summary>
        public bool TakeDamage(float damage)
        {
            if (_data == null || IsIndestructible || IsBroken) return false;

            if (damage <= _data.Toughness) return false;

            float effectiveDamage = damage - _data.Toughness;
            _currentDurability = Mathf.Max(0f, _currentDurability - effectiveDamage);
            OnDurabilityChanged?.Invoke(_currentDurability, MaxDurability);

            DebugLogger.Log(LOG_TAG,
                $"{_data.ItemName} 내구도 감소 — {effectiveDamage:F1} dmg → {_currentDurability:F1}/{MaxDurability:F1}");

            if (_currentDurability <= 0f)
            {
                DebugLogger.Log(LOG_TAG, $"{_data.ItemName} 파괴됨!");
                OnBroken?.Invoke(this);
            }

            return true;
        }

        /// <summary>
        /// 내구도를 직접 설정한다 (드롭 아이템 회수 시 동기화용).
        /// </summary>
        public void SetDurability(float value)
        {
            if (_data == null || IsIndestructible) return;
            _currentDurability = Mathf.Clamp(value, 0f, MaxDurability);
            OnDurabilityChanged?.Invoke(_currentDurability, MaxDurability);
        }

        /// <summary>
        /// 내구도를 회복한다 (수선).
        /// </summary>
        public void Repair(float amount)
        {
            if (_data == null || IsIndestructible) return;

            _currentDurability = Mathf.Min(MaxDurability, _currentDurability + amount);
            OnDurabilityChanged?.Invoke(_currentDurability, MaxDurability);

            DebugLogger.Log(LOG_TAG,
                $"{_data.ItemName} 수선 — +{amount:F1} → {_currentDurability:F1}/{MaxDurability:F1}");
        }
    }
}
