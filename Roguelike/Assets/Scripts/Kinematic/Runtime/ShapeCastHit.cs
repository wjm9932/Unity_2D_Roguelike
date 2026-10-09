using UnityEngine;

namespace Kinematic.Runtime
{
    public readonly struct ShapeCastHit
    {
        public KinematicBody Body { get; }
        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public float Distance { get; }
        internal float Fraction { get; }
        internal bool IsGrazing { get; }

        internal ShapeCastHit(KinematicBody body, Vector2 point, Vector2 normal, float distance, float fraction, bool isGrazing)
        {
            Body = body;
            Point = point;
            Normal = normal;
            Distance = distance;
            Fraction = fraction;
            IsGrazing = isGrazing;
        }
    }
}
