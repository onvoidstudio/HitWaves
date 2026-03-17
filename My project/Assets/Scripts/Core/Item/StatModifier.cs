using System;
using UnityEngine;

namespace HitWaves.Core.Item
{
    /// <summary>
    /// 아이템이 부여하는 스탯 수정자 1개.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("변경할 스탯 종류")]
        public StatType statType;

        [Tooltip("변경 수치 (양수=증가, 음수=감소)")]
        public float value;
    }
}
