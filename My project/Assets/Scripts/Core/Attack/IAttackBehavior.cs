using UnityEngine;

namespace HitWaves.Core.Attack
{
    /// <summary>
    /// 공격 방식 인터페이스. 근접/원거리/특수 공격 등 확장용.
    /// </summary>
    public interface IAttackBehavior
    {
        /// <summary>
        /// 공격 실행 후 쿨다운을 적용할지 여부.
        /// </summary>
        bool CooldownOnExecute { get; }

        /// <summary>
        /// 초기화.
        /// </summary>
        void Initialize(AttackHandler handler);

        /// <summary>
        /// 공격 실행. 히트한 대상 수를 반환한다.
        /// </summary>
        int Execute(AttackHandler handler, Vector2 direction);

        /// <summary>
        /// 정리 (오브젝트 파괴 등).
        /// </summary>
        void Cleanup();
    }
}
