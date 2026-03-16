using UnityEngine;
using HitWaves.Core;

namespace HitWaves.Entity.AI
{
    /// <summary>
    /// 이동 패턴 모듈 인터페이스.
    /// MonoBehaviour 컴포넌트로 구현하여 Inspector에서 조합 가능.
    /// </summary>
    public interface IMoveBehavior
    {
        /// <summary>
        /// 이 이동 모듈이 현재 활성 상태인지.
        /// EntityBrain이 비활성화된 모듈은 무시한다.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// 이동 방향과 속도를 계산하여 반환.
        /// EntityBrain이 FixedUpdate에서 호출한다.
        /// </summary>
        Vector2 GetDesiredVelocity(Rigidbody2D rb, StatHandler statHandler);
    }
}
