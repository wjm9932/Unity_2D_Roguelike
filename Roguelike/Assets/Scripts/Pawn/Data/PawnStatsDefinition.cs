using System;
using UnityEngine;

public interface IPawnStatsDefinition
{
    public float Speed { get; }
    public AnimationCurve AccelerationCurve { get; }
}

[Serializable]
public class PawnStatsDefinition : IPawnStatsDefinition
{
    [SerializeField] private float speed;
    [SerializeField] private AnimationCurve accelerationCurve;

    public float Speed => speed;
    public AnimationCurve AccelerationCurve => accelerationCurve;
}
