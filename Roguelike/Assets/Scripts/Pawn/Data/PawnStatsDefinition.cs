using System;
using UnityEngine;

public interface IPawnStatsDefinition
{

}

[Serializable]
public class PawnStatsDefinition : IPawnStatsDefinition
{
    [SerializeField] private float speed;
    [SerializeField] private AnimationCurve accelerationCurve;

    public float Speed => speed;
    public AnimationCurve AccelerationCurve => accelerationCurve;
}
