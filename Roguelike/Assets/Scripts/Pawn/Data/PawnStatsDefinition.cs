using System;
using UnityEngine;

namespace Pawn.Data
{
    public interface IPawnStatsDefinition
    {
        public float Speed { get; }
        public AnimationCurve ActiveAccelerationCurve { get; }
        public AnimationCurve PassiveDecelerationCurve { get; }
    }

    [Serializable]
    public class PawnStatsDefinition : IPawnStatsDefinition
    {
        [SerializeField] private float speed;
        [SerializeField] private AnimationCurve activeAccelerationCurve;
        [SerializeField] private AnimationCurve passiveDecelerationCurve;

        public float Speed => speed;
        public AnimationCurve ActiveAccelerationCurve => activeAccelerationCurve;
        public AnimationCurve PassiveDecelerationCurve => passiveDecelerationCurve;
    }
}
