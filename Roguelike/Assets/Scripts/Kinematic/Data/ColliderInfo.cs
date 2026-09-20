using CustomAttribute.Runtime;
using System;
using UnityEngine;

namespace Kinematic.Data
{
    public enum Shape
    {
        Circle,
        Box,
    }

    public readonly struct Contact
    {
        public float PenetrationDepth { get; }
        public Vector2 SeparationNormal { get; }
        public Vector2 SeparationMtv => SeparationNormal * PenetrationDepth;

        public Contact(Vector2 separationNormal, float penetrationDepth)
        {
            SeparationNormal = separationNormal;
            PenetrationDepth = penetrationDepth;
        }
    }

    [Serializable]
    public struct ColliderInfo
    {
        // 충돌체 타입
        [SerializeField] private Shape shape;
        // 오브젝트와 실제 충돌 중심과의 오프셋
        [SerializeField] private Vector2 offset;
        // Circle일 때 사용할 반지름
        [ShowIf(nameof(shape), (int)Shape.Circle)]
        [SerializeField] private float radius;
        // Box일 때 사용할 크기
        [ShowIf(nameof(shape), (int)Shape.Box)]
        [SerializeField] private Vector2 halfExtents;

        public Shape Shape => shape;
        public Vector2 Offset => offset;
        public float Radius => radius;
        public Vector2 HalfExtents => halfExtents;

        public static ColliderInfo CreateCircle(Vector2 offset, float radius)
        {
            return new ColliderInfo
            {
                shape = Shape.Circle,
                offset = offset,
                radius = radius,
            };
        }

        public static ColliderInfo CreateBox(Vector2 offset, Vector2 halfExtents)
        {
            return new ColliderInfo
            {
                shape = Shape.Box,
                offset = offset,
                halfExtents = halfExtents,
            };
        }
    }
}

