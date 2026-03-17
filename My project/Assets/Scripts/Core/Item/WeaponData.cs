using UnityEngine;

namespace HitWaves.Core.Item
{
    /// <summary>
    /// 무기 전용 데이터 SO. ItemData를 상속하여 무기 고유 속성을 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "New WeaponData", menuName = "HitWaves/WeaponData")]
    public class WeaponData : ItemData
    {
        [Header("무기 공통")]
        [Tooltip("무기 데미지")]
        [Min(0f)]
        [SerializeField] private float _damage = 10f;

        [Tooltip("사거리 (월드 유닛)")]
        [Min(0f)]
        [SerializeField] private float _range = 1.5f;

        [Tooltip("연사 속도 (초당 공격 횟수)")]
        [Min(0.01f)]
        [SerializeField] private float _fireRate = 2f;

        [Header("원거리 전용")]
        [Tooltip("투사체 속도 (0 = 근접 무기)")]
        [Min(0f)]
        [SerializeField] private float _projectileSpeed;

        [Tooltip("탄창 용량 (0 = 근접, -1 = 무한)")]
        [SerializeField] private int _magazineSize;

        [Tooltip("재장전 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _reloadTime = 1f;

        [Tooltip("투사체 크기 배율 (1 = 기본)")]
        [Min(0.1f)]
        [SerializeField] private float _projectileSize = 1f;

        [Tooltip("투사체 프리팹")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("무기 고유 넉백 힘 (투사체에 적용)")]
        [Min(0f)]
        [SerializeField] private float _knockbackForce;

        [Tooltip("관통 횟수 (0 = 첫 적에서 파괴, -1 = 무한 관통)")]
        [SerializeField] private int _pierceCount;

        // === 프로퍼티 ===
        public float Damage => _damage;
        public float Range => _range;
        public float FireRate => _fireRate;
        public float ProjectileSpeed => _projectileSpeed;
        public int MagazineSize => _magazineSize;
        public float ReloadTime => _reloadTime;
        public float ProjectileSize => _projectileSize;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float KnockbackForce => _knockbackForce;
        public int PierceCount => _pierceCount;

        public bool IsMelee => _projectileSpeed <= 0f;
        public bool IsRanged => _projectileSpeed > 0f;
    }
}
