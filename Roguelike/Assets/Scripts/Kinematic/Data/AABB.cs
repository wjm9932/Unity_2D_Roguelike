using UnityEngine;

namespace Kinematic.Data
{
    public readonly struct AABB
    {
        public Vector2 Min { get; }
        public Vector2 Max { get; }

        public AABB(Vector2 min, Vector2 max)
        {
            Debug.Assert(min.x <= max.x && min.y <= max.y, $"Invalid AABB range: Min {min}, Max {max}");

            Min = min;
            Max = max;
        }

        public static AABB Create(Vector2 center, Vector2 extents)
        {
            Debug.Assert(extents.x >= 0f && extents.y >= 0f, $"AABB extents must be non-negative: {extents}");

            return new AABB(center - extents, center + extents);
        }


        public bool Overlaps(in AABB other)
        {
            return Min.x <= other.Max.x &&
                   Max.x >= other.Min.x &&
                   Min.y <= other.Max.y &&
                   Max.y >= other.Min.y;
        }

        public bool Contains(in AABB other)
        {
            return Min.x <= other.Min.x &&
                   Max.x >= other.Max.x &&
                   Min.y <= other.Min.y &&
                   Max.y >= other.Max.y;
        }
    }
}
