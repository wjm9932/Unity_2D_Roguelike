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

        // 쿼드 트리 조회용 이동경로 AABB
        public AABB GetSweptBounds(Vector2 moveDelta)
        {
            var movedMin = Min + moveDelta;
            var movedMax = Max + moveDelta;

            return new AABB(Vector2.Min(Min, movedMin), Vector2.Max(Max, movedMax));
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
